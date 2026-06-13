using Scalar.AspNetCore;
using Serilog.Context;
using System.Security.Claims;

namespace BudgetFriend.API.Shared.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipline(this WebApplication app)
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
                LogContext.PushProperty("UserId", userId);

            await next();
        });

        return app;
    }
}
