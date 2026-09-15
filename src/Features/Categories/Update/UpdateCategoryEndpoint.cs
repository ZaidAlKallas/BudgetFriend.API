namespace BudgetFriend.API.Features.Categories.Update;

public static class UpdateCategoryEndpoint
{
    public static void MapUpdateCategory(this IEndpointRouteBuilder app) =>
        app.MapPut("/{categoryId}", HandleAsync)
            .WithValidation<UpdateCategoryRequest>()
            .WithName("Update Category")
            .WithSummary("Update a category by its ID")
            .WithDescription("Updates a category by its ID")
            .Produces<UpdateCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    public static async Task<IResult> HandleAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var exists = await dbContext.Categories
            .AnyAsync(c => c.Name == normalizedName
                && c.TransactionType == request.TransactionType
                && c.UserId == currentUser.UserId
                && c.Id != categoryId,
                cancellationToken);

        if (exists)
            return Results.Conflict($"A category with the name '{normalizedName}' and type '{request.TransactionType}' already exists.");

        var updated = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId && c.Id == categoryId)
            .ExecuteUpdateAsync(c => c
                .SetProperty(x => x.Name, normalizedName)
                .SetProperty(x => x.TransactionType, request.TransactionType),
                cancellationToken);

        if (updated == 0)
            return Results.NotFound();

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        return Results.Ok(new UpdateCategoryResponse(categoryId, normalizedName, request.TransactionType));
    }
}
