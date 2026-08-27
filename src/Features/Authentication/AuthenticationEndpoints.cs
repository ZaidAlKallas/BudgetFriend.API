using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Logout;
using BudgetFriend.API.Features.Authentication.Refresh;
using BudgetFriend.API.Features.Authentication.Register;

namespace BudgetFriend.API.Features.Authentication;

public static class AuthenticationEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapAuthenticationEndpoints()
        {
            var group = app.MapGroup("/api/v{version:apiVersion}/auth")
                .WithTags("Authentication");

            group.MapRegisterEndpoint();
            group.MapLoginEndpoint();
            group.MapRefreshEndpoint();
            group.MapLogoutEndpoint();
            group.MapProfileEndpoint();

            return app;
        }
    }
}
