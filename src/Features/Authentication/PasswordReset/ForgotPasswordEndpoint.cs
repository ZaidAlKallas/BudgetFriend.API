using BudgetFriend.API.Shared.Email;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public static class ForgotPasswordEndpoint
{
    public static void MapForgotPasswordEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/forgot-password", HandleAsync)
            .WithValidation<ForgotPasswordRequest>()
            .RequireRateLimiting("EmailPolicy")
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset code")
            .WithDescription("Sends a password reset code to the given address if an account exists")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        AppDbContext dbContext,
        IEmailSender emailSender,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Results.Ok(new { message = "If this email belongs to an account, a password reset code has been sent." });
        }

        var code = SecurityTokens.GenerateNumericCode();
        user.PasswordResetCodeHash = SecurityTokens.Hash(code);
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.Add(PasswordResetDefaults.Expiry);
        user.PasswordResetAttemptCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email,
                "Reset your password",
                AuthEmailBuilder.BuildPasswordResetCodeMessage(code),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return Results.Ok(new { message = "If this email belongs to an account, a password reset code has been sent." });
    }
}