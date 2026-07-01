using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Dashboards.GetCategoriesAnalysis;

public sealed record GetCategoriesAnalysisResponse(List<CategoryAnalysis> CategoryBreakdown);

public sealed record CategoryAnalysis(
    Guid CategoryId,
    string CategoryName,
    TransactionType TransactionType,
    List<CategoryCurrencyBreakdown> CurrencyBreakdown);

public sealed record CategoryCurrencyBreakdown(
    Currency Currency,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage);
