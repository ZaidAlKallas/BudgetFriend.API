using BudgetFriend.API.Features.Authentication.Jwt;
using BudgetFriend.API.Features.Authentication.RefreshToken;
using BudgetFriend.API.Shared.Validation;

namespace BudgetFriend.API.Features.Authentication.Refresh;

public static class RefreshEndpoint
{
    public static void MapRefreshEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/refresh", HandleAsync)
        .WithValidation<RefreshRequest>()
        .WithName("Refresh")
        .WithSummary("Refresh access token")
        .WithDescription("Exchanges a valid refresh token for a new access token and refresh token pair")
        .Produces<Login.LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<IResult> HandleAsync(
        RefreshRequest request,
        IRefreshTokenService refreshTokenService,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var user = await refreshTokenService.ConsumeAsync(request.RefreshToken, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Refresh token rejected: invalid, used, revoked, or expired");
            return Results.Unauthorized();
        }

        var (accessToken, jwtId) = jwtTokenGenerator.Generate(user);
        var (newRefreshToken, expiresAtUtc) = await refreshTokenService.GenerateAsync(user, jwtId, cancellationToken);

        logger.LogInformation("User {UserId} refreshed token successfully", user.Id);
        return Results.Ok(new Login.LoginResponse(accessToken, newRefreshToken, expiresAtUtc));
    }
}
