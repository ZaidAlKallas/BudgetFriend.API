using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Shared.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Authentication.Register;

public static partial class RegisterEndpoint {
    /// <summary>
    /// Maps the user registration endpoint.
    /// </summary>
    /// <param name="app">The application to map the endpoint on.</param>
    public static void MapRegisterEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            AppDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            CancellationToken cancellationToken) => {
                // Check existing user
                var email = request.Email.Trim();
                var normalizedEmail = email.ToUpperInvariant();

                var exists = await dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

                if (exists) {
                    return Results.Conflict(new { message = "A user with this email already exists." });
                }

                var user = new User {
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

                var response = new RegisterResponse(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.CreatedAtUtc);

                return Results.Created($"/api/users/{user.Id}", response);

            })
        .AddEndpointFilter<FluentValidationFilter<RegisterRequest>>()
        .WithName("Register")
        .WithSummary("Register a new user")
        .WithDescription("Creates a new account using email and password")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);
}
