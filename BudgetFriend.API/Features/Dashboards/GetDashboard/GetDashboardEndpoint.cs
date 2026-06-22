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
                    - a.Transactions.Where(t => t.Category.TransactionType == TransactionType.Expense).Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

        var totalBalance = accountSummaries.Sum(a => a.Balance);

        var monthlyIncome = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.Category.TransactionType == TransactionType.Income
                && t.TransactionDate >= startOfMonth)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var monthlyExpenses = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.Category.TransactionType == TransactionType.Expense
                && t.TransactionDate >= startOfMonth)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var topCategoriesData = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.Category.TransactionType == TransactionType.Expense
                && t.TransactionDate >= startOfMonth)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topCategories = topCategoriesData
            .Select(x => new CategorySpending(
                x.CategoryId,
                x.Name,
                x.TotalAmount,
                x.TransactionCount))
            .ToList();

        var recentTransactions = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new RecentTransaction(
                t.Id,
                t.AccountId,
                t.Account.Name,
                t.CategoryId,
                t.Category.Name,
                t.Category.TransactionType,
                t.Amount,
                t.Note,
                t.TransactionDate))
            .ToListAsync(cancellationToken);

        var response = new GetDashboardResponse(
            totalBalance,
            monthlyIncome,
            monthlyExpenses,
            monthlyIncome - monthlyExpenses,
            accountSummaries,
            topCategories,
            recentTransactions);

        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Results.Ok(response);
    }
}
