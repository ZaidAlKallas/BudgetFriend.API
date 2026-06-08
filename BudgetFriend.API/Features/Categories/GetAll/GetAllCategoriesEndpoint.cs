using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Categories.GetAll;

public static class GetAllCategoriesEndpoint
{
    public static void MapGetAllCategories(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Categories")
            .WithSummary("Get all categories for the current user")
            .WithDescription("Retrieves a list of all categories associated with the current user")
            .Produces<List<GetCategoryResponse>>(StatusCodes.Status200OK);

    public static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId)
            .OrderBy(c => c.TransactionType)
            .ThenBy(c => c.Name)
            .Select(c => new GetCategoryResponse(c.Id, c.Name, c.TransactionType))
            .ToListAsync(cancellationToken);

        return Results.Ok(categories);
    }
}
