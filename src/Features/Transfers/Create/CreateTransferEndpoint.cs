namespace BudgetFriend.API.Features.Transfers.Create;

public static class CreateTransferEndpoint
{
    public static void MapCreateTransfer(this IEndpointRouteBuilder app) =>
        app.MapPost("/", HandleAsync)
            .WithValidation<CreateTransferRequest>()
            .WithName("Create Transfer")
            .WithSummary("Create a new transfer between accounts")
            .WithDescription("Creates a transfer between two accounts belonging to the current user, generating TransferOut and TransferIn transactions")
            .Produces<CreateTransferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        CreateTransferRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .Where(a => a.UserId == currentUser.UserId
                && (a.Id == request.FromAccountId || a.Id == request.ToAccountId))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        if (!accounts.TryGetValue(request.FromAccountId, out var fromAccount))
            return Results.NotFound(new { message = "Source account not found." });

        if (!accounts.ContainsKey(request.ToAccountId))
            return Results.NotFound(new { message = "Destination account not found." });

        var fromAccountBalance = await dbContext.Transactions
            .Where(t => t.AccountId == request.FromAccountId)
            .SumAsync(t => (t.TransactionType == TransactionType.TransferOut || t.TransactionType == TransactionType.Expense) ?
                    -t.Amount : t.Amount, cancellationToken) + fromAccount.InitialBalance;
        if (fromAccountBalance < request.FromAmount)
            return Results.BadRequest(new { message = "Insufficient balance in the source account." });

        var transferOutTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.FromAccountId,
            Amount = request.FromAmount,
            TransactionType = TransactionType.TransferOut,
            Note = request.Note?.Trim(),
            TransactionDate = request.TransferDate ?? DateTime.UtcNow,
        };

        var transferInTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.ToAccountId,
            Amount = request.ToAmount,
            TransactionType = TransactionType.TransferIn,
            Note = request.Note?.Trim(),
            TransactionDate = request.TransferDate ?? DateTime.UtcNow,
        };

        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            OutgoingTransactionId = transferOutTransaction.Id,
            IncomingTransactionId = transferInTransaction.Id,
            FromAmount = request.FromAmount,
            ToAmount = request.ToAmount,
            Note = request.Note?.Trim(),
            TransferDate = request.TransferDate ?? DateTime.UtcNow,
        };

        dbContext.Transactions.Add(transferOutTransaction);
        dbContext.Transactions.Add(transferInTransaction);
        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        BudgetFriendMetrics.TransfersCreated.Add(1);

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation(
            "Transfer {TransferId} created from account {FromAccountId} to {ToAccountId} by user {UserId}",
            transfer.Id, request.FromAccountId, request.ToAccountId, currentUser.UserId);

        return Results.Created(
            $"/api/transfers/{transfer.Id}",
            new CreateTransferResponse(
                transfer.Id,
                transfer.FromAccountId,
                transfer.ToAccountId,
                transfer.FromAmount,
                transfer.ToAmount,
                transfer.Note,
                transfer.TransferDate,
                transfer.CreatedAtUtc));
    }
}
