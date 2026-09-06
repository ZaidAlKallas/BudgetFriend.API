using BudgetFriend.API.Shared.Email;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public static class ResendVerificationEndpoint
{
    public static void MapResendVerificationEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/resend-verification", HandleAsync)
            .WithValidation<ResendVerificationRequest>()
            .RequireRateLimiting("EmailPolicy")
            .WithName("ResendVerification")
            .WithSummary("Resend email verification")
            .WithDescription("Sends a new email verification link to the given address if an account exists")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> HandleAsync(
        ResendVerificationRequest request,
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

        if (user is null || user.IsEmailVerified)
        {
            return Results.Ok(new { message = "If this email belongs to an account, a verification link has been sent." });
        }

        var token = SecurityTokens.Generate();
        user.EmailVerificationTokenHash = SecurityTokens.Hash(token);
        user.EmailVerificationExpiresAtUtc = DateTime.UtcNow.AddHours(24);

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email,
                "Verify your email",
                AuthEmailBuilder.BuildVerificationMessage(emailOptions, token),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
        }

        return Results.Ok(new { message = "If this email belongs to an account, a verification link has been sent." });
    }
}