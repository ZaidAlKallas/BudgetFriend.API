using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Dashboards.GetDashboard;

public sealed record GetDashboardResponse(
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal NetMonthlyIncome,
    List<AccountSummary> Accounts,
    List<CategorySpending> TopExpenseCategories,
    List<RecentTransaction> RecentTransactions);

public sealed record AccountSummary(
    Guid Id,
    string Name,
    decimal Balance);

public sealed record CategorySpending(
    Guid CategoryId,
    string CategoryName,
    decimal TotalAmount,
    int TransactionCount);

public sealed record RecentTransaction(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    TransactionType TransactionType,
    decimal Amount,
    string? Note,
    DateTime TransactionDate);
