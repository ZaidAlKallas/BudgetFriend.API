using BudgetFriend.API.Features.Authentication.Google;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public sealed class ThrowingGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken)
        => throw new NotSupportedException("Unexpected Google ID token validation failure.");
}