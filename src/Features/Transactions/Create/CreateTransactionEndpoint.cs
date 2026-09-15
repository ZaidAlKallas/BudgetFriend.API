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
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<IResult> HandleAsync(
        CreateTransactionRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var accountExists = await dbContext.Accounts
            .AnyAsync(a => a.Id == request.AccountId && a.UserId == currentUser.UserId,
                cancellationToken);

        if (!accountExists)
            return Results.NotFound(new { message = "Account not found." });

        var category = await dbContext.Categories
            .Where(c => c.Id == request.CategoryId && c.UserId == currentUser.UserId)
            .Select(c => new { c.TransactionType })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return Results.NotFound(new { message = "Category not found." });

        if (category.TransactionType != request.TransactionType)
            return Results.BadRequest(new { message = "Category type does not match the transaction type." });

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            TransactionType = request.TransactionType,
            Note = request.Note?.Trim(),
            TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        BudgetFriendMetrics.TransactionsCreated.Add(1);

        await CacheInvalidation.InvalidateFinancialDataAsync(cacheService, currentUser.UserId, cancellationToken);

        logger.LogInformation(
            "Transaction {TransactionId} created for account {AccountId} by user {UserId}",
            transaction.Id, transaction.AccountId, currentUser.UserId);

        return Results.Created(
            $"/api/transactions/{transaction.Id}",
            new CreateTransactionResponse(
                transaction.Id,
                transaction.AccountId,
                transaction.CategoryId.GetValueOrDefault(),
                transaction.Amount,
                transaction.Note,
                transaction.TransactionDate,
                transaction.CreatedAtUtc));
    }
}
