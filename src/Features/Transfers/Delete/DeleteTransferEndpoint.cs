namespace BudgetFriend.API.Features.Transfers.Delete;

public static class DeleteTransferEndpoint
{
    public static void MapDeleteTransfer(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{transferId}", HandleAsync)
            .WithName("Delete Transfer by Id")
            .WithSummary("Delete a transfer by its ID")
            .WithDescription("Deletes a transfer and its associated TransferIn/TransferOut transactions")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid transferId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var transfer = await dbContext.Transfers
            .Where(t => t.FromAccount.UserId == currentUser.UserId && t.Id == transferId)
            .Select(t => new { t.OutgoingTransactionId, t.IncomingTransactionId })
            .FirstOrDefaultAsync(cancellationToken);

        if (transfer is null)
            return Results.NotFound();

        await dbContext.Transfers
            .Where(t => t.Id == transferId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.Id == transfer.OutgoingTransactionId || t.Id == transfer.IncomingTransactionId)
            .ExecuteDeleteAsync(cancellationToken);

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation("Transfer {TransferId} deleted by user {UserId}", transferId, currentUser.UserId);
        return Results.NoContent();
    }
}
