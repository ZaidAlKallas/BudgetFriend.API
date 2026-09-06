namespace BudgetFriend.API.Features.Authentication.Google;

public interface IGoogleIdTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken);
}