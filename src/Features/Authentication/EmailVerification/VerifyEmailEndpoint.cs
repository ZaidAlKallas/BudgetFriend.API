namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public static class VerifyEmailEndpoint
{
    public static void MapVerifyEmailEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/verify-email", HandleAsync)
            .WithValidation<VerifyEmailRequest>()
            .RequireRateLimiting("VerifyEmailPolicy")
            .WithName("VerifyEmail")
            .WithSummary("Verify user email")
            .WithDescription("Verifies a user's email address using a 6-digit code sent by email")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        VerifyEmailRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var codeHash = SecurityTokens.Hash(request.Code);

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.IsEmailVerified || user.EmailVerificationCodeHash is null)
        {
            return Results.BadRequest(new { message = "The verification code is invalid." });
        }

        if (user.EmailVerificationCodeExpiresAtUtc < DateTime.UtcNow)
        {
            ClearCode(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "The verification code has expired. Please request a new one." });
        }

        if (user.EmailVerificationAttemptCount >= EmailVerificationDefaults.MaxAttempts)
        {
            ClearCode(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "Too many failed attempts. Please request a new verification code." });
        }

        if (!string.Equals(user.EmailVerificationCodeHash, codeHash, StringComparison.OrdinalIgnoreCase))
        {
            user.EmailVerificationAttemptCount++;

            if (user.EmailVerificationAttemptCount >= EmailVerificationDefaults.MaxAttempts)
            {
                ClearCode(user);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.BadRequest(new { message = "Too many failed attempts. Please request a new verification code." });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var remaining = EmailVerificationDefaults.MaxAttempts - user.EmailVerificationAttemptCount;
            return Results.BadRequest(new { message = $"The verification code is invalid. {remaining} attempt(s) remaining." });
        }

        user.IsEmailVerified = true;
        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        ClearCode(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Email verified successfully." });
    }

    private static void ClearCode(User user)
    {
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAtUtc = null;
        user.EmailVerificationAttemptCount = 0;
    }
}