using BudgetFriend.API.Database;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Authentication;

public static class ProfileEndpoint
{
    public static void MapProfileEndpoint(this RouteGroupBuilder group) =>
        group.MapGet("/profile", HandlerAsync)
        .RequireAuthorization()
        .WithSummary("Get User Profile")
        .WithDescription("Retrieves the profile information for the authenticated user")
        .Produces<ProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandlerAsync(
        ICurrentUser currentUser,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {

        var userId = currentUser.UserId;

        var user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new ProfileResponse(
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.IsEmailVerified,
                x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return Results.Ok(user) ?? Results.NotFound();
    }
}

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified,
    DateTime CreatedAtUtc);

