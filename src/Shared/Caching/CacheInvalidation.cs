namespace BudgetFriend.API.Shared.Caching;

public static class CacheInvalidation
{
    public static async Task InvalidateFinancialDataAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await InvalidateDashboardAsync(cacheService, userId, cancellationToken);
        await InvalidateSummaryAsync(cacheService, userId, cancellationToken);
    }

    public static async Task InvalidateDashboardAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cacheService.RemoveAsync(CacheKeys.Dashboard(userId), cancellationToken);
    }

    public static async Task InvalidateSummaryAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cacheService.RemoveByPrefixAsync(CacheKeys.SummaryPrefix(userId), cancellationToken);
    }
}

