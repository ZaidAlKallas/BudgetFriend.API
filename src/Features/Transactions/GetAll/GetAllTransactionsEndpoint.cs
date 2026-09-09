namespace BudgetFriend.API.Features.Transactions.GetAll;

public static class GetAllTransactionsEndpoint
{
    public static void MapGetAllTransactions(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Transactions")
            .WithSummary("Get all transactions for the current user")
            .WithDescription("Retrieves paginated and filtered transactions across all accounts for the current user")
            .Produces<PaginatedResult<GetTransactionResponse>>(StatusCodes.Status200OK);

    public static async Task<IResult> HandleAsync(
        [AsParameters] TransactionFilters filters,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId);

        query = ApplyFilters(query, filters);

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

        var result = new PaginatedResult<GetTransactionResponse>(
            transactions,
            filters.PageNumber,
            filters.ValidPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)filters.ValidPageSize));

        return Results.Ok(result);
    }

    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionFilters filters)
    {
        if (filters.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filters.AccountId.Value);

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
