using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using Microsoft.EntityFrameworkCore;

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
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Categories
            .AnyAsync(c => c.Name == request.Name
                && c.TransactionType == request.TransactionType
                && c.UserId == currentUser.UserId
                && c.Id != categoryId,
                cancellationToken);

        if (exists)
            return Results.Conflict($"A category with the name '{request.Name}' and type '{request.TransactionType}' already exists.");

        var updated = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId && c.Id == categoryId)
            .ExecuteUpdateAsync(c => c
                .SetProperty(x => x.Name, request.Name.Trim())
                .SetProperty(x => x.TransactionType, request.TransactionType),
                cancellationToken);

        return updated == 0
            ? Results.NotFound()
            : Results.Ok(new UpdateCategoryResponse(categoryId, request.Name.Trim(), request.TransactionType));
    }
}
