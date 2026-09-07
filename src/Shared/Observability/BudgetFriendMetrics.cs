using System.Diagnostics.Metrics;

namespace BudgetFriend.API.Shared.Observability;

public static class BudgetFriendMetrics
{
    public const string MeterName = "BudgetFriend";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> AccountsCreated = Meter.CreateCounter<long>(
        "budgetfriend.accounts.created",
        description: "Number of accounts created");

    public static readonly Counter<long> TransactionsCreated = Meter.CreateCounter<long>(
        "budgetfriend.transactions.created",
        description: "Number of transactions created");

    public static readonly Counter<long> TransfersCreated = Meter.CreateCounter<long>(
        "budgetfriend.transfers.created",
        description: "Number of transfers created");
}