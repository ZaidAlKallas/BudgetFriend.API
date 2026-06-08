using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
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
        CancellationToken cancellationToken)
    {
        var deletedRows = await dbContext.Categories
            .Where(c => c.UserId == currentUser.UserId && c.Id == categoryId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
    }
}
