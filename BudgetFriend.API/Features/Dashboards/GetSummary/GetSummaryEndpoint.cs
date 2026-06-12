using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication;
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
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.TransactionType })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.TransactionType,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync(cancellationToken);

        var totalIncome = categoryData
            .Where(c => c.TransactionType == TransactionType.Income)
            .Sum(c => c.TotalAmount);

        var totalExpenses = categoryData
            .Where(c => c.TransactionType == TransactionType.Expense)
            .Sum(c => c.TotalAmount);

        var categoryBreakdown = categoryData
            .Select(c => new CategorySummary(
                c.CategoryId,
                c.Name,
                c.TransactionType,
                c.TotalAmount,
                c.TransactionCount,
                c.TransactionType == TransactionType.Income
                    ? (totalIncome > 0 ? Math.Round(c.TotalAmount / totalIncome * 100, 1) : 0)
                    : (totalExpenses > 0 ? Math.Round(c.TotalAmount / totalExpenses * 100, 1) : 0)))
            .ToList();

        return Results.Ok(new GetSummaryResponse(totalIncome, totalExpenses, totalIncome - totalExpenses, categoryBreakdown));
    }
}
