using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Caching;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetFriend.API.IntegrationTests.Caching;

public sealed class RedisUnavailableCacheFixture : IAsyncLifetime
{
    private const string DeadRedisConnectionString = "127.0.0.1:1,connectTimeout=250,connectRetry=0";

    public BudgetFriendApiFactory Factory { get; }

    public RedisUnavailableCacheFixture()
    {
        Factory = new BudgetFriendApiFactory();
        Factory.UseRealRedis(DeadRedisConnectionString);
    }

    public ValueTask InitializeAsync() => Factory.InitializeAsync();

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

public sealed class RedisUnavailableCacheTests(RedisUnavailableCacheFixture fixture)
    : IClassFixture<RedisUnavailableCacheFixture>
{
    private readonly BudgetFriendApiFactory _factory = fixture.Factory;

    [Fact]
    public async Task SetThenGet_ShouldFallBackToInMemory_WhenRedisConfiguredButUnavailable()
    {
        var cache = _factory.Services.GetRequiredService<ICacheService>();
        var key = CacheKeys.Summary(Guid.NewGuid());

        await cache.SetAsync(key, "cached-value", TimeSpan.FromMinutes(5));

        var cached = await cache.GetAsync<string>(key);

        cached.Should().Be("cached-value");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldWork_WhenRedisConfiguredButUnavailable()
    {
        var cache = _factory.Services.GetRequiredService<ICacheService>();
        var userId = Guid.NewGuid();
        var summaryKey = CacheKeys.Summary(userId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var dashboardKey = CacheKeys.Dashboard(userId);

        await cache.SetAsync(summaryKey, "summary", TimeSpan.FromMinutes(5));
        await cache.SetAsync(dashboardKey, "dashboard", TimeSpan.FromMinutes(5));

        await cache.RemoveByPrefixAsync(CacheKeys.SummaryPrefix(userId));

        (await cache.GetAsync<string>(summaryKey)).Should().BeNull();
        (await cache.GetAsync<string>(dashboardKey)).Should().Be("dashboard");
    }
}