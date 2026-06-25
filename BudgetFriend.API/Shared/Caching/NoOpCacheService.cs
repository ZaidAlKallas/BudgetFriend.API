namespace BudgetFriend.API.Shared.Caching;

public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}