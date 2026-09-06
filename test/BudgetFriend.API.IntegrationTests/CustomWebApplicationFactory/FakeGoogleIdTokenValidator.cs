using BudgetFriend.API.Features.Authentication.Google;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public static GoogleUserInfo? Result { get; set; }

    public Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Result ?? throw new InvalidOperationException("FakeGoogleIdTokenValidator.Result is not set."));
    }
}