using Asp.Versioning;
using BudgetFriend.API.Features.Authentication.Google;
using BudgetFriend.API.Features.Authentication.Jwt;
using BudgetFriend.API.Features.Authentication.RefreshToken;
using BudgetFriend.API.Shared.Caching;
using BudgetFriend.API.Shared.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

namespace BudgetFriend.API.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services,
        ConfigurationManager configuration)
    {

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.UseNpgsql(
                configuration.GetConnectionString("Database"));
        });

        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services,
        ConfigurationManager configuration)
    {

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("Jwt configuration is missing.");

                if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
                    throw new InvalidOperationException("Jwt:SecretKey is required.");

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtOptions.SecretKey))
                    };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later."
                }, cancellationToken);
            };

            options.AddFixedWindowLimiter("LoginPolicy", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("EmailPolicy", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(10);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });
        return services;
    }

    public static IServiceCollection AddEmailing(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        if (configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()?.UseConsoleEmailSender ?? true)
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        else
            services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    public static IServiceCollection AddGoogleAuth(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));

        services.AddSingleton(new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true }));

        services.AddScoped<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        var healthChecks = services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
            healthChecks.AddRedis(redisConnectionString);

        return services;
    }

    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }

    public static IServiceCollection AddCaching(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddMemoryCache();

        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Redis")))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis");
                var redisConfig = ConfigurationOptions.Parse(connectionString!);
                redisConfig.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(redisConfig);
            });

            services.AddStackExchangeRedisCache(_ => { });

            services.AddOptions<RedisCacheOptions>()
                .Configure<IConnectionMultiplexer>((options, multiplexer) =>
                    options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer));
        }

        services.AddSingleton<ICacheService>(sp => new HybridCacheService(
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetService<IDistributedCache>(),
            sp.GetService<IConnectionMultiplexer>(),
            sp.GetRequiredService<ILogger<HybridCacheService>>()));

        return services;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: configuration["OTEL_SERVICE_NAME"] ?? "BudgetFriend.API",
                serviceVersion: typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter("System.Runtime")
                .AddMeter(BudgetFriendMetrics.MeterName)
                .AddOtlpExporter());

        return services;
    }
}
