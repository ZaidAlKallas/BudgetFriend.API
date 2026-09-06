using BudgetFriend.API.Features.Authentication.RefreshToken;
using Microsoft.AspNetCore.Identity;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public static class ResetPasswordEndpoint
{
    public static void MapResetPasswordEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/reset-password", HandleAsync)
            .WithValidation<ResetPasswordRequest>()
            .WithName("ResetPassword")
            .WithSummary("Reset password with a token")
            .WithDescription("Sets a new password using a token sent by email and revokes existing sessions")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IRefreshTokenService refreshTokenService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var tokenHash = SecurityTokens.Hash(request.Token);

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash, cancellationToken);

        if (user is null)
        {
            return Results.BadRequest(new { message = "The reset token is invalid or has already been used." });
        }

        if (user.PasswordResetExpiresAtUtc < DateTime.UtcNow)
        {
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAtUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "The reset token has expired. Please request a new password reset link." });
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

        logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        return Results.Ok(new { message = "Password reset successfully." });
    }
}