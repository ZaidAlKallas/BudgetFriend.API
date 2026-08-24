using BudgetFriend.API.Features.Authentication.RefreshToken;

namespace BudgetFriend.API.Features.Authentication.Logout;

public static class LogoutEndpoint
{
    public static void MapLogoutEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/logout", HandleAsync)
        .RequireAuthorization()
        .WithName("Logout")
        .WithSummary("Logout user")
        .WithDescription("Revokes all refresh tokens for the authenticated user")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        IRefreshTokenService refreshTokenService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        await refreshTokenService.RevokeAllForUserAsync(userId, cancellationToken);

        logger.LogInformation("User {UserId} logged out", userId);
        return Results.NoContent();
    }
}
