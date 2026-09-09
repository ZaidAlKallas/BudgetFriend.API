using BudgetFriend.API.Features.Transactions;

namespace BudgetFriend.API.Features.Accounts.GetTransactions;

public static class GetAccountTransactionsEndpoint
{
    public static void MapGetAccountTransactions(this IEndpointRouteBuilder app) =>
        app.MapGet("/{accountId}/transactions", HandleAsync)
            .WithName("Get Account Transactions")
            .WithSummary("Get account info and transactions")
            .WithDescription("Retrieves account details along with paginated and filtered transactions, including the balance for filtered results")
            .Produces<GetAccountTransactionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid accountId,
        [AsParameters] AccountTransactionFilters filters,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .Where(a => a.Id == accountId && a.UserId == currentUser.UserId)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.InitialBalance,
                a.Currency,
                CurrentBalance = a.Transactions.Sum(t =>
                    t.TransactionType == TransactionType.Income ||
                    t.TransactionType == TransactionType.TransferIn
                        ? t.Amount
                        : -t.Amount) + a.InitialBalance
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Results.NotFound();

        var query = dbContext.Transactions
            .Where(t => t.AccountId == accountId);

        query = ApplyFilters(query, filters);

        var filteredBalance = await query.SumAsync(t =>
            t.TransactionType == TransactionType.Income ||
            t.TransactionType == TransactionType.TransferIn
                ? t.Amount
                : -t.Amount, cancellationToken);

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip(filters.Skip)
            .Take(filters.ValidPageSize)
            .Select(t => new GetTransactionResponse(
                t.Id,
                t.AccountId,
                t.Account.Name,
                t.CategoryId.GetValueOrDefault(),
                t.Category != null ? t.Category.Name : null,
                t.Account.Currency,
                t.Amount,
                t.Note,
                t.TransactionDate,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var paginatedTransactions = new PaginatedResult<GetTransactionResponse>(
            transactions,
            filters.PageNumber,
            filters.ValidPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)filters.ValidPageSize));

        var result = new GetAccountTransactionsResponse(
            account.Id,
            account.Name,
            account.InitialBalance,
            account.CurrentBalance,
            filteredBalance,
            account.Currency,
            paginatedTransactions);

        return Results.Ok(result);
    }

    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        AccountTransactionFilters filters)
    {
        if (filters.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filters.CategoryId.Value);

        if (filters.TransactionType.HasValue)
            query = query.Where(t => t.TransactionType == filters.TransactionType.Value);

        if (filters.DateFrom.HasValue)
            query = query.Where(t => t.TransactionDate >= filters.DateFrom.Value);

        if (filters.DateTo.HasValue)
            query = query.Where(t => t.TransactionDate <= filters.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(t => t.Note != null && t.Note.Contains(filters.Search));

        return query;
    }
}
