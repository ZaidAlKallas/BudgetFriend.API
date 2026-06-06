using Microsoft.AspNetCore.RateLimiting;

namespace BudgetFriend.API.Shared.Extensions;

public static class WebApplicationExtensions {
    public static WebApplication ConfigureRateLimiter(this WebApplication app) {

        app.UseRateLimiter(new RateLimiterOptions() {
            OnRejected = async (context, cancellationToken) => {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new {
                    error = "Too many requests. Please try again later."
                }, cancellationToken);
            },
            RejectionStatusCode = StatusCodes.Status429TooManyRequests
        });

        return app;
    }
}