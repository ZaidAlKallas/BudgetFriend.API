namespace BudgetFriend.API.Shared.Caching;

public static class CacheKeys
{
    public static string Dashboard(Guid userId)
        => $"dashboard:{userId}";

    public static string Summary(Guid userId)
        => $"summary:{userId}:default:default";

    public static string Summary(Guid userId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.ToString("yyyyMMdd") ?? "default";
        var to = toDate?.ToString("yyyyMMdd") ?? "default";
        return $"summary:{userId}:{from}:{to}";
    }

    public static string SummaryPrefix(Guid userId) => $"summary:{userId}:";
}
