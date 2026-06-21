namespace BudgetFriend.API.Shared.Caching;

public static class CacheKeys
{
    public static string Dashboard(Guid userId)
        => $"dashboard:{userId}";

    public static string Summary(Guid userId)
        => $"summary:{userId}";
}
