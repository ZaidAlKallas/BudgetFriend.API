using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Caching;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Dashboards.GetSummary;

public static class GetSummaryEndpoint
{
    public static void MapGetSummary(this IEndpointRouteBuilder app) =>
        app.MapGet("/summary", HandleAsync)
            .WithName("Get Summary")
            .WithSummary("Get financial summary for a period")
            .WithDescription("Returns a financial summary for the specified period including income, expenses, net amount, and category breakdown. Defaults to the current month if no dates are provided.")
            .Produces<GetSummaryResponse>(StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        DateTime? fromDate,
        DateTime? toDate,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var cacheKey = CacheKeys.Summary(userId);
        var cachedSummary = await cacheService.GetAsync<GetSummaryResponse>(cacheKey, cancellationToken);
        if (cachedSummary is not null)
        {
            return Results.Ok(cachedSummary);
        }

        var now = DateTime.UtcNow;
        var from = fromDate?.ToUniversalTime() ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = toDate?.ToUniversalTime() ?? now;

        var accountCurrencies = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Currency)
            .Select(g => new { Currency = g.Key, InitialBalance = g.Sum(a => a.InitialBalance) })
            .ToListAsync(cancellationToken);

        var transactionData = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.TransactionDate >= from
                && t.TransactionDate <= to)
            .GroupBy(t => t.Account.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                Income = g.Where(t => t.Category.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Category.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var txByCurrency = transactionData.ToDictionary(x => x.Currency);

        var currencySummaries = accountCurrencies
            .Select(ac =>
            {
                var tx = txByCurrency.GetValueOrDefault(ac.Currency);
                var inc = tx?.Income ?? 0m;
                var exp = tx?.Expense ?? 0m;
                var openingBalance = fromDate is null ? ac.InitialBalance : 0;
                return new CurrencySummary(ac.Currency, ac.InitialBalance, inc, exp, openingBalance + inc - exp);
            })
            .ToList();

        var response = new GetSummaryResponse(currencySummaries);

        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Results.Ok(response);
    }
}
