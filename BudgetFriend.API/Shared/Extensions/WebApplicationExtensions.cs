using Scalar.AspNetCore;

namespace BudgetFriend.API.Shared.Extensions;

public static class WebApplicationExtensions {
    public static WebApplication ConfigurePipline(this WebApplication app) {

        if (app.Environment.IsDevelopment()) {
            app.MapOpenApi();
            app.MapScalarApiReference(option => {
                option.PersistentAuthentication = true;
                option.AddAuthorizationCodeFlow("Bearer", config => {
                    config.Token = "Bearer {token}";
                    config.TokenName = "Authorization";
                });
            });
        }

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}