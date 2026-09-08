using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace BudgetFriend.API.Shared.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> ConfigurePipeline(this WebApplication app)
    {

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(option =>
            {
                option.PersistentAuthentication = true;
                option.AddAuthorizationCodeFlow("Bearer", config =>
                {
                    config.Token = "Bearer {token}";
                    config.TokenName = "Authorization";
                });
            });

            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                Log.Information("Applying database migrations...");
                await db.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to apply database migrations.");
                throw;
            }
        }

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        app.Use(async (context, next) =>
        {
            var activity = Activity.Current;
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var scope = new List<IDisposable>();

            if (activity is not null)
            {
                scope.Add(LogContext.PushProperty("TraceId", activity.TraceId.ToString()));
                scope.Add(LogContext.PushProperty("SpanId", activity.SpanId.ToString()));
            }

            if (userId is not null)
                scope.Add(LogContext.PushProperty("UserId", userId));

            if (scope.Count == 0)
            {
                await next();
                return;
            }

            try
            {
                await next();
            }
            finally
            {
                for (var i = scope.Count - 1; i >= 0; i--)
                    scope[i].Dispose();
            }
        });

        return app;
    }
}
