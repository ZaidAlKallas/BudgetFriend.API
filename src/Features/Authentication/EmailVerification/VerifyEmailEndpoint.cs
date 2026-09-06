namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public static class VerifyEmailEndpoint
{
    public static void MapVerifyEmailEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/verify-email", HandleAsync)
            .WithValidation<VerifyEmailRequest>()
            .WithName("VerifyEmail")
            .WithSummary("Verify user email")
            .WithDescription("Verifies a user's email address using a token sent by email")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<IResult> HandleAsync(
        VerifyEmailRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tokenHash = SecurityTokens.Hash(request.Token);

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(
                u => u.EmailVerificationTokenHash == tokenHash && !u.IsEmailVerified,
                cancellationToken);

        if (user is null)
        {
            return Results.BadRequest(new { message = "The verification token is invalid or has already been used." });
        }

        if (user.EmailVerificationExpiresAtUtc < DateTime.UtcNow)
        {
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationExpiresAtUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.BadRequest(new { message = "The verification token has expired. Please request a new verification email." });
        }

        user.IsEmailVerified = true;
        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationExpiresAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Email verified successfully." });
    }
}