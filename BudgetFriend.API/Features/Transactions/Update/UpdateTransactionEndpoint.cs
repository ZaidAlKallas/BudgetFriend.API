using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Transactions.Update;

public static class UpdateTransactionEndpoint
{
    public static void MapUpdateTransaction(this IEndpointRouteBuilder app) =>
        app.MapPut("/{transactionId}", HandleAsync)
            .WithValidation<UpdateTransactionRequest>()
            .WithName("Update Transaction")
            .WithSummary("Update a transaction by its ID")
            .WithDescription("Updates the amount, note, or date of a transaction")
            .Produces<UpdateTransactionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        Guid transactionId,
        UpdateTransactionRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId && t.Id == transactionId)
            .Select(t => new { t.Id, t.AccountId, t.CategoryId, t.CreatedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
            return Results.NotFound();

        var updated = await dbContext.Transactions
            .Where(t => t.Id == transactionId)
            .ExecuteUpdateAsync(t => t
                .SetProperty(x => x.Amount, request.Amount)
                .SetProperty(x => x.Note, request.Note?.Trim())
                .SetProperty(x => x.TransactionDate, request.TransactionDate),
                cancellationToken);

        return updated == 0
            ? Results.NotFound()
            : Results.Ok(new UpdateTransactionResponse(
                transaction.Id,
                transaction.AccountId,
                transaction.CategoryId,
                request.Amount,
                request.Note?.Trim(),
                request.TransactionDate,
                transaction.CreatedAtUtc));
    }
}
