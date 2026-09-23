using BudgetFriend.API.Shared.Email;

namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public static class ResendVerificationEndpoint
{
    public static void MapResendVerificationEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/resend-verification", HandleAsync)
            .WithValidation<ResendVerificationRequest>()
            .RequireRateLimiting("EmailPolicy")
            .WithName("ResendVerification")
            .WithSummary("Resend email verification")
            .WithDescription("Sends a new verification code to the given address if an account exists")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        ResendVerificationRequest request,
        AppDbContext dbContext,
        IEmailSender emailSender,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.IsEmailVerified)
        {
            return Results.Ok(new { message = "If this email belongs to an account, a new verification code has been sent." });
        }

        var code = SecurityTokens.GenerateNumericCode();
        user.EmailVerificationCodeHash = SecurityTokens.Hash(code);
        user.EmailVerificationCodeExpiresAtUtc = DateTime.UtcNow.Add(EmailVerificationDefaults.Expiry);
        user.EmailVerificationAttemptCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email,
                "Verify your email",
                AuthEmailBuilder.BuildVerificationCodeMessage(code),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
        }

        return Results.Ok(new { message = "If this email belongs to an account, a new verification code has been sent." });
    }
}