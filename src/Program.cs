using BudgetFriend.API.Features.Accounts;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Categories;
using BudgetFriend.API.Features.Dashboards;
using BudgetFriend.API.Features.Transactions;
using BudgetFriend.API.Features.Transfers;
using BudgetFriend.API.Shared.Errors;
using BudgetFriend.API.Shared.Extensions;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SpanId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/budgetfriend-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {TraceId} {SpanId} {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "BudgetFriend")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddLoginRateLimiting();

builder.Services.AddEmailing(builder.Configuration);

builder.Services.AddGoogleAuth(builder.Configuration);

builder.Services.AddHttpContextAccessor()
    .AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddHealthChecks(builder.Configuration);

builder.Services.AddCaching(builder.Configuration);

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddObservability(builder.Configuration);

builder.Services.AddCustomApiVersioning();

var app = builder.Build();

await app.ConfigurePipeline();

app.MapAuthenticationEndpoints();
app.MapAccountEndpoints();
app.MapCategoryEndpoints();
app.MapTransactionEndpoints();
app.MapTransferEndpoints();
app.MapDashboardEndpoints();
app.MapHealthChecks("/_health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();

