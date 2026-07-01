using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Caching;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Dashboards.GetDashboard;

public static class GetDashboardEndpoint
{
    public static void MapGetDashboard(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Dashboard")
            .WithSummary("Get dashboard overview")
            .WithDescription("Returns an overview of the user's financial status including total balance, monthly income/expenses, account summaries, top spending categories, and recent transactions")
            .Produces<GetDashboardResponse>(StatusCodes.Status200OK);

    private const int _dashboardRecentTransactions = 5;
    private const int _dashboardTopCategories = 5;

    private static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var cacheKey = CacheKeys.Dashboard(userId);
        var cachedDashboard = await cacheService.GetAsync<GetDashboardResponse>(cacheKey, cancellationToken);
        if (cachedDashboard is not null)
        {
            return Results.Ok(cachedDashboard);
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var accountSummaries = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new AccountSummary(
                a.Id,
                a.Name,
                a.InitialBalance
                    + a.Transactions.Where(t => t.Category.TransactionType == TransactionType.Income).Sum(t => t.Amount)
                    - a.Transactions.Where(t => t.Category.TransactionType == TransactionType.Expense).Sum(t => t.Amount),
                a.Currency))
            .ToListAsync(cancellationToken);

        var monthlyByCurrency = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId && t.TransactionDate >= startOfMonth)
            .GroupBy(t => t.Account.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                Income = g.Where(t => t.Category.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Category.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var topCategoriesData = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.Category.TransactionType == TransactionType.Expense
                && t.TransactionDate >= startOfMonth)
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Account.Currency })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.Currency,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var frequentExpenseCategories = topCategoriesData
            .GroupBy(x => new { x.CategoryId, x.Name })
            .Select(g => new CategoryOverview(
                g.Key.CategoryId,
                g.Key.Name,
                [.. g.Select(x => new CategoryCurrencyBreakdown(x.Currency, x.TotalAmount, x.TransactionCount))]))
            .OrderByDescending(c => c.AmountsByCurrency.Sum(x => x.Count))
            .Take(_dashboardTopCategories)
            .ToList();

        var recentTransactions = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(_dashboardRecentTransactions)
            .Select(t => new RecentTransaction(
                t.Id,
                t.AccountId,
                t.Account.Name,
                t.Account.Currency,
                t.CategoryId,
                t.Category.Name,
                t.Category.TransactionType,
                t.Amount,
                t.Note,
                t.TransactionDate))
            .ToListAsync(cancellationToken);

        var monthlyIncomeByCurrency = monthlyByCurrency.ToDictionary(x => x.Currency, x => x.Income);
        var monthlyExpensesByCurrency = monthlyByCurrency.ToDictionary(x => x.Currency, x => x.Expense);

        var currencyBreakdown = accountSummaries
            .GroupBy(a => a.Currency)
            .Select(g =>
            {
                var inc = monthlyIncomeByCurrency.GetValueOrDefault(g.Key, 0m);
                var exp = monthlyExpensesByCurrency.GetValueOrDefault(g.Key, 0m);
                return new CurrencyOverview(g.Key, g.Sum(a => a.Balance), inc, exp, inc - exp);
            })
            .ToList();

        var response = new GetDashboardResponse(
            accountSummaries,
            frequentExpenseCategories,
            recentTransactions,
            currencyBreakdown);

        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Results.Ok(response);
    }
}
