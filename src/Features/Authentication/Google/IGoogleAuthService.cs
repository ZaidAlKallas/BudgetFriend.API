namespace BudgetFriend.API.Features.Authentication.Google;

public interface IGoogleAuthService
{
    Task<GoogleAuthResult> AuthenticateAsync(string idToken, CancellationToken cancellationToken);
}