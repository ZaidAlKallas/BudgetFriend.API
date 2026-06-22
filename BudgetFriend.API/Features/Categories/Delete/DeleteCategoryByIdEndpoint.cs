using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Caching;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Categories.Delete;

public static class DeleteCategoryByIdEndpoint
{
    public static void MapDeleteCategory(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{categoryId}", HandleAsync)
            .WithName("Delete Category by Id")
            .WithSummary("Delete a category by its ID")
            .WithDescription("Deletes a category associated with the specified ID")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid categoryId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var deletedRows = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId && c.Id == categoryId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedRows == 0)
            return Results.NotFound();

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation("Category {CategoryId} deleted by user {UserId}", categoryId, currentUser.UserId);
        return Results.NoContent();
    }
}
