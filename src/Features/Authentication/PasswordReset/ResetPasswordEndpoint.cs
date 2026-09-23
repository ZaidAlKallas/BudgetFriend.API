using BudgetFriend.API.Features.Authentication.RefreshToken;
using Microsoft.AspNetCore.Identity;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public static class ResetPasswordEndpoint
{
    public static void MapResetPasswordEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/reset-password", HandleAsync)
            .WithValidation<ResetPasswordRequest>()
            .RequireRateLimiting("ResetPasswordPolicy")
            .WithName("ResetPassword")
            .WithSummary("Reset password with a code")
            .WithDescription("Sets a new password using a code sent by email and revokes existing sessions")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IRefreshTokenService refreshTokenService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var codeHash = SecurityTokens.Hash(request.Code);

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.PasswordResetCodeHash is null)
        {
            return Results.BadRequest(new { message = "The reset code is invalid." });
        }

        if (user.PasswordResetCodeExpiresAtUtc < DateTime.UtcNow)
        {
            ClearCode(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "The reset code has expired. Please request a new one." });
        }

        if (user.PasswordResetAttemptCount >= PasswordResetDefaults.MaxAttempts)
        {
            ClearCode(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "Too many failed attempts. Please request a new reset code." });
        }

        if (!string.Equals(user.PasswordResetCodeHash, codeHash, StringComparison.OrdinalIgnoreCase))
        {
            user.PasswordResetAttemptCount++;

            if (user.PasswordResetAttemptCount >= PasswordResetDefaults.MaxAttempts)
            {
                ClearCode(user);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.BadRequest(new { message = "Too many failed attempts. Please request a new reset code." });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var remaining = PasswordResetDefaults.MaxAttempts - user.PasswordResetAttemptCount;
            return Results.BadRequest(new { message = $"The reset code is invalid. {remaining} attempt(s) remaining." });
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        ClearCode(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

        logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        return Results.Ok(new { message = "Password reset successfully." });
    }

    private static void ClearCode(User user)
    {
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.PasswordResetAttemptCount = 0;
    }
}