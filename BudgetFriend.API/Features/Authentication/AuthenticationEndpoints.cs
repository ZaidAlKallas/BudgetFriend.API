using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;

namespace BudgetFriend.API.Features.Authentication;

public static class AuthenticationEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapAuthenticationEndpoints()
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Authentication");

            group.MapRegisterEndpoint();
            group.MapLoginEndpoint();
            group.MapProfileEndpoint();

            return app;
        }
    }
}
