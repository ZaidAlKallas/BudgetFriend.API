using BudgetFriend.API.Database;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
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
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                using var _ = LogContext.PushProperty("UserId", userId);
                await next();
                return;
            }

            await next();
        });

        return app;
    }
}
