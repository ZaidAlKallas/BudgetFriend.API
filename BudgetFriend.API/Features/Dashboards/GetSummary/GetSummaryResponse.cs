using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Dashboards.GetSummary;

public sealed record GetSummaryResponse(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    List<CategorySummary> CategoryBreakdown);

public sealed record CategorySummary(
    Guid CategoryId,
    string CategoryName,
    TransactionType TransactionType,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage);
