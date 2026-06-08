using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Categories.Create;

public static class CreateCategoryEndpoint
{
    public static void MapCreateCategory(this IEndpointRouteBuilder app) =>
        app.MapPost("/", HandleAsync)
            .WithValidation<CreateCategoryRequest>()
            .WithName("Create Category")
            .WithSummary("Create a new category")
            .WithDescription("Creates a new category with a name and transaction type")
            .Produces<CreateCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateCategoryRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Categories
            .AnyAsync(c => c.UserId == currentUser.UserId
                && c.Name == request.Name
                && c.TransactionType == request.TransactionType,
                cancellationToken);

        if (exists)
            return Results.Conflict($"A category with the name '{request.Name}' and type '{request.TransactionType}' already exists.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId,
            Name = request.Name.Trim(),
            TransactionType = request.TransactionType,
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/categories/{category.Id}",
            new CreateCategoryResponse(category.Id, category.Name, category.TransactionType));
    }
}
