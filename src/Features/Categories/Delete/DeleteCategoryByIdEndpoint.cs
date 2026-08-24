namespace BudgetFriend.API.Features.Categories.Delete;

public static class DeleteCategoryByIdEndpoint
{
    public static void MapDeleteCategory(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{categoryId}", HandleAsync)
            .WithName("Delete Category by Id")
            .WithSummary("Delete a category by its ID")
            .WithDescription("Deletes a category associated with the specified ID")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    public static async Task<IResult> HandleAsync(
        Guid categoryId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == categoryId && c.UserId == currentUser.UserId, cancellationToken);

        if (!categoryExists)
            return Results.NotFound();

        var isUsedByTransactions = await dbContext.Transactions
            .AnyAsync(t => t.CategoryId == categoryId, cancellationToken);

        if (isUsedByTransactions)
            return Results.Conflict(new { message = "Category cannot be deleted because it is used by existing transactions." });

        await dbContext.Categories
            .Where(c => c.Id == categoryId)
            .ExecuteDeleteAsync(cancellationToken);

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation("Category {CategoryId} deleted by user {UserId}", categoryId, currentUser.UserId);
        return Results.NoContent();
    }
}
