using BudgetFriend.API.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BudgetFriend.API.Features.Authentication;

public static class ProfileEndpoint {
    public static void MapProfileEndpoint(this RouteGroupBuilder group) =>
        group.MapGet("/api/profile", async (
            ClaimsPrincipal principal,
            AppDbContext dbContext,
            CancellationToken cancellationToken) => {
                var userId = Guid.Parse(
                    principal.FindFirstValue(
                        ClaimTypes.NameIdentifier)!);

                var user = await dbContext.Users
                    .AsNoTracking()
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
        .RequireAuthorization();
}

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified,
    DateTime CreatedAtUtc);

