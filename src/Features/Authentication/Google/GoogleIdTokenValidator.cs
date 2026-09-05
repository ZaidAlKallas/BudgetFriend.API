using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BudgetFriend.API.Features.Authentication.Google;

public sealed class GoogleIdTokenValidator(
    IOptions<GoogleOptions> options,
    ConfigurationManager<OpenIdConnectConfiguration> configurationManager) : IGoogleIdTokenValidator
{
    private static readonly string[] _validIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ClientId))
        {
            throw new InvalidOperationException("Google:ClientId is not configured.");
        }

        var openIdConfig = await configurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = _validIssuers,
            ValidateAudience = true,
            ValidAudience = options.Value.ClientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = openIdConfig.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(idToken, validationParameters);

        if (!result.IsValid)
        {
            throw new InvalidOperationException("Google ID token validation failed.");
        }

        var claims = result.ClaimsIdentity?.Claims.ToList() ?? [];
        var subject = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new InvalidOperationException("Google ID token is missing the subject claim.");

        var emailConfirmed = bool.TryParse(
            claims.FirstOrDefault(c => c.Type == "email_verified")?.Value,
            out var emailVerified) && emailVerified;

        return new GoogleUserInfo(
            subject,
            claims.FirstOrDefault(c => c.Type == "email")?.Value,
            emailConfirmed,
            claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
            claims.FirstOrDefault(c => c.Type == "family_name")?.Value);
    }
}