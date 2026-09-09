using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BudgetFriend.API.Shared.Caching;

public class HybridCacheService : ICacheService
{
    private const string MemoryKeyPrefix = "bf-cache:";
    private const int RedisBackend = 1;
    private const int MemoryBackend = 0;
    private const long RedisRetryIntervalMs = 30_000;

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _memoryKeys = new();

    private int _backend = MemoryBackend;
    private long _lastRedisAttemptTimestamp;

    public HybridCacheService(
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache,
        IConnectionMultiplexer? redis,
        ILogger<HybridCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _backend = redis is not null ? RedisBackend : MemoryBackend;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (ShouldTryRedis())
        {
            try
            {
                var value = await GetFromRedisAsync<T>(key, cancellationToken);
                PromoteToRedis();
                return value;
            }
            catch (RedisException ex)
            {
                SwitchToMemory(ex);
            }
        }

        return GetFromMemory<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (ShouldTryRedis())
        {
            try
            {
                await SetInRedisAsync(key, value, expiration, cancellationToken);
                PromoteToRedis();
                return;
            }
            catch (RedisException ex)
            {
                SwitchToMemory(ex);
            }
        }

        SetInMemory(key, value, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemoveFromMemory(key);

        if (ShouldTryRedis())
        {
            try
            {
                await _distributedCache!.RemoveAsync(key, cancellationToken);
                PromoteToRedis();
                return;
            }
            catch (RedisException ex)
            {
                SwitchToMemory(ex);
            }
        }
    }

    public async Task RemoveByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        RemoveFromMemoryByPrefix(keyPrefix);

        if (ShouldTryRedis() && _redis is not null)
        {
            try
            {
                var endPoint = _redis.GetEndPoints().FirstOrDefault();
                if (endPoint is null)
                    return;

                var server = _redis.GetServer(endPoint);
                var keys = server.Keys(pattern: $"{keyPrefix}*").ToArray();

                if (keys.Length > 0)
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(keys);
                }

                PromoteToRedis();
            }
            catch (RedisException ex)
            {
                SwitchToMemory(ex);
            }
        }
    }

    private async Task<T?> GetFromRedisAsync<T>(string key, CancellationToken cancellationToken)
    {
        var json = await _distributedCache!.GetStringAsync(key, cancellationToken);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    private async Task SetInRedisAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        var json = JsonSerializer.Serialize(value);

        await _distributedCache!.SetStringAsync(key, json, options, cancellationToken);
    }

    private T? GetFromMemory<T>(string key)
        => _memoryCache.TryGetValue(MemoryKeyPrefix + key, out object? value) && value is T typed
            ? typed
            : default;

    private void SetInMemory<T>(string key, T value, TimeSpan expiration)
    {
        var memoryKey = MemoryKeyPrefix + key;

        _memoryCache.Set(memoryKey, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        _memoryKeys[memoryKey] = 0;
    }

    private void RemoveFromMemory(string key)
    {
        var memoryKey = MemoryKeyPrefix + key;

        _memoryCache.Remove(memoryKey);
        _memoryKeys.TryRemove(memoryKey, out _);
    }

    private void RemoveFromMemoryByPrefix(string keyPrefix)
    {
        var memoryKeyPrefix = MemoryKeyPrefix + keyPrefix;

        foreach (var key in _memoryKeys.Keys)
        {
            if (key.StartsWith(memoryKeyPrefix, StringComparison.Ordinal))
            {
                _memoryCache.Remove(key);
                _memoryKeys.TryRemove(key, out _);
            }
        }
    }

    private bool ShouldTryRedis()
    {
        if (_redis is null || _distributedCache is null)
            return false;

        if (_backend == RedisBackend)
            return true;

        var now = Environment.TickCount64;
        var lastAttempt = Interlocked.Read(ref _lastRedisAttemptTimestamp);

        if (now - lastAttempt < RedisRetryIntervalMs)
            return false;

        Interlocked.CompareExchange(ref _lastRedisAttemptTimestamp, now, lastAttempt);
        return true;
    }

    private void SwitchToMemory(RedisException ex)
    {
        if (Interlocked.Exchange(ref _backend, MemoryBackend) != MemoryBackend)
            _logger.LogWarning(ex, "Redis unavailable; switching cache to in-memory fallback.");
    }

    private void PromoteToRedis()
    {
        if (Interlocked.Exchange(ref _backend, RedisBackend) != RedisBackend)
            _logger.LogInformation("Redis connection recovered; cache re-enabled on Redis.");
    }
}