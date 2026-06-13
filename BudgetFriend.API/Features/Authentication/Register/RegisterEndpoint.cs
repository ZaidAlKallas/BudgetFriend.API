using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Shared.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created();
    }
}
