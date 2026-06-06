using BudgetFriend.API.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BudgetFriend.API.Features.Authentication;

public static class ProfileEndpoint {
    public static void MapProfileEndpoint(this RouteGroupBuilder group) =>
        group.MapGet("/profile", async (
            ClaimsPrincipal principal,
            AppDbContext dbContext,
            CancellationToken cancellationToken) => {
                var userId = Guid.Parse(
                    principal.FindFirstValue(
                        ClaimTypes.NameIdentifier)!);

                var user = await dbContext.Users
                    .Where(x => x.Id == userId)
                    .Select(x => new ProfileResponse(
                        x.Id,
                        x.Email,
                        x.FirstName,
                        x.LastName,
                        x.IsEmailVerified,
                        x.CreatedAtUtc))
                    .SingleAsync(cancellationToken);

                return Results.Ok(user);
            })
        .RequireAuthorization()
        .WithSummary("Get User Profile")
        .WithDescription("Retrieves the profile information for the authenticated user")
        .Produces<ProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
}

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified,
    DateTime CreatedAtUtc);

