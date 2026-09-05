using BudgetFriend.API.Shared.Email;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public static class ForgotPasswordEndpoint
{
    public static void MapForgotPasswordEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/forgot-password", HandleAsync)
            .WithValidation<ForgotPasswordRequest>()
            .RequireRateLimiting("EmailPolicy")
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset link")
            .WithDescription("Sends a password reset link to the given address if an account exists")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        AppDbContext dbContext,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Results.Ok(new { message = "If this email belongs to an account, a password reset link has been sent." });
        }

        var token = SecurityTokens.Generate();
        user.PasswordResetTokenHash = SecurityTokens.Hash(token);
        user.PasswordResetExpiresAtUtc = DateTime.UtcNow.AddHours(1);

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email,
                "Reset your password",
                AuthEmailBuilder.BuildPasswordResetMessage(emailOptions, token),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return Results.Ok(new { message = "If this email belongs to an account, a password reset link has been sent." });
    }
}