using BudgetFriend.API.Features.Authentication.Google;
using Microsoft.AspNetCore.Http;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public sealed class ThrowingBadRequestGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken)
        => throw new BadHttpRequestException("Malformed Google ID token.", StatusCodes.Status400BadRequest);
}
