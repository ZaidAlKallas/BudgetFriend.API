using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Dashboards.GetDashboard;

public sealed record GetDashboardResponse(
    List<AccountSummary> Accounts,
    List<CategoryOverview> TopExpenseCategories,
    List<RecentTransaction> RecentTransactions,
    List<CurrencyOverview> CurrencyBreakdown);

public sealed record AccountSummary(
    Guid Id,
    string Name,
    decimal Balance,
    Currency Currency);

public sealed record CurrencyOverview(
    Currency Currency,
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal NetMonthlyIncome);

public sealed record CategoryOverview(
    Guid CategoryId,
    string CategoryName,
    List<CategoryCurrencyBreakdown> AmountsByCurrency);

public sealed record CategoryCurrencyBreakdown(
    Currency Currency,
    decimal TotalAmount,
    int Count);

public sealed record RecentTransaction(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Currency Currency,
    Guid CategoryId,
    string CategoryName,
    TransactionType TransactionType,
    decimal Amount,
    string? Note,
    DateTime TransactionDate);
