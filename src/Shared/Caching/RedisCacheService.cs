using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace BudgetFriend.API.Shared.Caching;

public class RedisCacheService(IDistributedCache cache, IConnectionMultiplexer? redis = null) : ICacheService
{
    private readonly IDistributedCache _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);

        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        var json = JsonSerializer.Serialize(value);

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        if (redis is null)
            return;

        var endPoint = redis.GetEndPoints().FirstOrDefault();
        if (endPoint is null)
            return;

        var server = redis.GetServer(endPoint);
        var keys = server.Keys(pattern: $"{keyPrefix}*").ToArray();

        if (keys.Length > 0)
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(keys);
        }
    }
}
