namespace BudgetFriend.API.Features.Transactions.Delete;

public static class DeleteTransactionByIdEndpoint
{
    public static void MapDeleteTransaction(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{transactionId}", HandleAsync)
            .WithName("Delete Transaction by Id")
            .WithSummary("Delete a transaction by its ID")
            .WithDescription("Deletes a transaction associated with the specified ID")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid transactionId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId && t.Id == transactionId)
            .Select(t => new { t.Id, t.TransactionType })
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
            return Results.NotFound();

        if (transaction.TransactionType is TransactionType.TransferIn or TransactionType.TransferOut)
            return Results.Conflict("Transfer transactions can only be removed through the transfer endpoint.");

        var deletedRows = await dbContext.Transactions
            .Where(t => t.Id == transactionId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedRows == 0)
            return Results.NotFound();

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} deleted by user {UserId}", transactionId, currentUser.UserId);
        return Results.NoContent();
    }
}
