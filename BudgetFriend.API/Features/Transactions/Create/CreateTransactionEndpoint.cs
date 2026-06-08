using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Transactions.Create;

public static class CreateTransactionEndpoint
{
    public static void MapCreateTransaction(this IEndpointRouteBuilder app) =>
        app.MapPost("/", HandleAsync)
            .WithValidation<CreateTransactionRequest>()
            .WithName("Create Transaction")
            .WithSummary("Create a new transaction")
            .WithDescription("Creates a new transaction for an account")
            .Produces<CreateTransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        CreateTransactionRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var accountExists = await dbContext.Accounts
            .AnyAsync(a => a.Id == request.AccountId && a.UserId == currentUser.UserId,
                cancellationToken);

        if (!accountExists)
            return Results.NotFound(new { message = "Account not found." });

        var categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.UserId == currentUser.UserId,
                cancellationToken);

        if (!categoryExists)
            return Results.NotFound(new { message = "Category not found." });

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Note = request.Note?.Trim(),
            TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Transaction {TransactionId} created for account {AccountId} by user {UserId}",
            transaction.Id, transaction.AccountId, currentUser.UserId);

        return Results.Created(
            $"/api/transactions/{transaction.Id}",
            new CreateTransactionResponse(
                transaction.Id,
                transaction.AccountId,
                transaction.CategoryId,
                transaction.Amount,
                transaction.Note,
                transaction.TransactionDate,
                transaction.CreatedAtUtc));
    }
}
