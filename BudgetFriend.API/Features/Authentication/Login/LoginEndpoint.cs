using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Authentication.Jwt;
using BudgetFriend.API.Shared.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Features.Authentication.Login;

public static class LoginEndpoint {
    /// <summary>
    /// Maps the user login endpoint.
    /// </summary>
    /// <param name="app">The application to map the endpoint on.</param>
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/login", HandleAsync)
        .WithValidation<LoginRequest>()
        .RequireRateLimiting("LoginPolicy")
        .WithName("Login")
        .WithSummary("Login to the application")
        .WithDescription("Authenticates a user and returns a JWT token")
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .Produces<LoginResponse>(StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken) {

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users.SingleOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null)
            return Results.Unauthorized();

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password) == PasswordVerificationResult.Failed)
            return Results.Unauthorized();

        var accessToken = jwtTokenGenerator.Generate(user);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        return Results.Ok(new LoginResponse(accessToken, expiresAtUtc));
    }
}