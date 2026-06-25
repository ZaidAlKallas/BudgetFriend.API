namespace BudgetFriend.API.Shared.Caching;

public static class CacheInvalidation
{
    public static async Task InvalidateFinancialDataAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await InvalidateDashboardAsync(cacheService, userId, cancellationToken);

        await InvalidateSummarydAsync(cacheService, userId, cancellationToken);

    }

    public static async Task InvalidateDashboardAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cacheService.RemoveAsync(CacheKeys.Dashboard(userId), cancellationToken);
    }

    public static async Task InvalidateSummarydAsync(
        ICacheService cacheService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cacheService.RemoveAsync(CacheKeys.Summary(userId), cancellationToken);
    }
}

