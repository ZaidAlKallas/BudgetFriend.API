using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Categories.GetById;

public static class GetCategoryByIdEndpoint {
    public static void MapGetCategoryById(this IEndpointRouteBuilder app) =>
        app.MapGet("/{categoryId}", HandleAsync)
            .WithName("Get Category by Id")
            .WithSummary("Get a category by its ID")
            .WithDescription("Retrieves a category associated with the specified ID")
            .Produces<GetCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid categoryId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) {
        var category = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId && c.Id == categoryId)
            .Select(c => new GetCategoryResponse(c.Id, c.Name, c.TransactionType))
            .FirstOrDefaultAsync(cancellationToken);

        return category is not null ? Results.Ok(category) : Results.NotFound();
    }
}
