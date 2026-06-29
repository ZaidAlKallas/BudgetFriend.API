using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Dashboards.GetSummary;

public sealed record GetSummaryResponse(
    List<CurrencySummary> Summaries);

public sealed record CurrencySummary(
    Currency Currency,
    decimal InitialBalance,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount);
