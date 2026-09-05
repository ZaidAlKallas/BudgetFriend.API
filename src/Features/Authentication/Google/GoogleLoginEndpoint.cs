using BudgetFriend.API.Features.Authentication.Jwt;
using BudgetFriend.API.Features.Authentication.RefreshToken;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BudgetFriend.API.Features.Authentication.Google;

public static class GoogleLoginEndpoint
{
    public static void MapGoogleLoginEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/google", HandleAsync)
            .WithValidation<GoogleLoginRequest>()
            .WithName("GoogleLogin")
            .WithSummary("Sign in with Google")
            .WithDescription("Authenticates a user using a Google ID token and returns the application's JWT credentials")
            .Produces<Login.LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        GoogleLoginRequest request,
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        GoogleAuthResult result;
        try
        {
            result = await googleAuthService.AuthenticateAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException
            or OpenIdConnectProtocolException
            or System.Net.Http.HttpRequestException)
        {
            logger.LogWarning(ex, "Google authentication rejected an ID token");
            return Results.BadRequest(new { message = "The Google credential is invalid or could not be validated." });
        }

        if (!result.Success || result.User is null)
        {
            logger.LogWarning("Google authentication rejected an ID token: {Reason}", result.Error);
            return Results.Conflict(new { message = result.Error ?? "Unable to sign in with Google." });
        }

        var (accessToken, jwtId) = jwtTokenGenerator.Generate(result.User);
        var (refreshToken, expiresAtUtc) = await refreshTokenService.GenerateAsync(result.User, jwtId, cancellationToken);

        logger.LogInformation("User {UserId} signed in with Google", result.User.Id);
        return Results.Ok(new Login.LoginResponse(accessToken, refreshToken, expiresAtUtc));
    }
}