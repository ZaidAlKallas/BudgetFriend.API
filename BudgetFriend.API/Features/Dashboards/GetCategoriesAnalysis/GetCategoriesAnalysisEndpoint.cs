using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Dashboards.GetCategoriesAnalysis;

public static class GetCategoriesAnalysisEndpoint
{
    public static void MapGetCategoriesAnalysis(this IEndpointRouteBuilder app) =>
        app.MapGet("/categories-analysis", HandleAsync)
            .WithName("Get Categories Analysis")
            .WithSummary("Get category breakdown for a period")
            .WithDescription("Returns a breakdown of income and expenses by category for the specified period. Defaults to the current month if no dates are provided.")
            .Produces<GetCategoriesAnalysisResponse>(StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        DateTime? fromDate,
        DateTime? toDate,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;
        var from = fromDate?.ToUniversalTime() ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = toDate?.ToUniversalTime() ?? now;

        var categoryData = await dbContext.Transactions
            .Where(t => t.Account.UserId == userId
                && t.TransactionDate >= from
                && t.TransactionDate <= to)
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.TransactionType, t.Account.Currency })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.TransactionType,
                g.Key.Currency,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var totalsByTypeAndCurrency = categoryData
            .GroupBy(x => (x.Currency, x.TransactionType))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

        var categoryBreakdown = categoryData
            .GroupBy(x => new { x.CategoryId, x.Name, x.TransactionType })
            .Select(g =>
            {
                var currencyBreakdowns = g.Select(x =>
                {
                    var total = totalsByTypeAndCurrency.GetValueOrDefault((x.Currency, x.TransactionType), 0m);
                    var percentage = total > 0 ? Math.Round(x.TotalAmount / total * 100, 1) : 0m;
                    return new CategoryCurrencyBreakdown(x.Currency, x.TotalAmount, x.TransactionCount, percentage);
                }).ToList();

                return new CategoryAnalysis(g.Key.CategoryId, g.Key.Name, g.Key.TransactionType, currencyBreakdowns);
            })
            .OrderByDescending(c => c.CurrencyBreakdown.Sum(x => x.TotalAmount))
            .ToList();

        return Results.Ok(new GetCategoriesAnalysisResponse(categoryBreakdown));
    }
}
