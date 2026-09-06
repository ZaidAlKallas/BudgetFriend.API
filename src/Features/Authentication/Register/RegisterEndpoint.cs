using BudgetFriend.API.Shared.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Features.Authentication.Register;

public static partial class RegisterEndpoint
{
    /// <summary>
    /// Maps the user registration endpoint.
    /// </summary>
    /// <param name="app">The application to map the endpoint on.</param>
    public static void MapRegisterEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/register", HandleAsync)
        .WithValidation<RegisterRequest>()
        .WithName("Register")
        .WithSummary("Register a new user")
        .WithDescription("Creates a new account using email and password")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {

        logger.LogInformation("Registering new user with email {Email}", request.Email);

        // Check existing user
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        var exists = await dbContext.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (exists)
        {
            return Results.Conflict(new { message = "A user with this email already exists." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            IsEmailVerified = false,
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var verificationToken = SecurityTokens.Generate();
        user.EmailVerificationTokenHash = SecurityTokens.Hash(verificationToken);
        user.EmailVerificationExpiresAtUtc = DateTime.UtcNow.AddHours(24);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email,
                "Verify your email",
                AuthEmailBuilder.BuildVerificationMessage(emailOptions, verificationToken),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
        }

        return Results.Created();
    }
}
