using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Transactions.Create;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Caching;
using FluentAssertions;
using StackExchange.Redis;
using System.Net;
using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace BudgetFriend.API.IntegrationTests.Caching;

public sealed class RedisBackedCacheFixture : IAsyncLifetime
{
    private readonly IContainer _redis = new ContainerBuilder()
        .WithImage("redis:8-alpine")
        .WithPortBinding(6379, true)
        .WithCleanUp(true)
        .Build();

    public BudgetFriendApiFactory Factory { get; private set; } = null!;

    public string RedisConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();
        RedisConnectionString = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}";
        await WaitUntilRedisReadyAsync();
        Factory = new BudgetFriendApiFactory();
        Factory.UseRealRedis(RedisConnectionString);
        await Factory.InitializeAsync();
    }

    private async Task WaitUntilRedisReadyAsync()
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var redis = await ConnectionMultiplexer.ConnectAsync($"{RedisConnectionString},abortConnect=true");
                await redis.GetDatabase().PingAsync();
                return;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        throw new TimeoutException("Redis did not become ready in time.");
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _redis.DisposeAsync();
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        using var redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
        return await redis.GetDatabase().KeyExistsAsync(key);
    }
}

public sealed class RedisBackedCacheTests(RedisBackedCacheFixture fixture)
    : IClassFixture<RedisBackedCacheFixture>
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();
    private readonly RedisBackedCacheFixture _fixture = fixture;

    [Fact]
    public async Task GetSummary_ShouldWriteToRedis_WhenRedisAvailable()
    {
        var token = await SetupUserAsync("redis-summary@example.com");
        await SetupDataAsync(token);
        var user = await TestDb.FindUserAsync(_fixture.Factory, "redis-summary@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Summary);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _fixture.KeyExistsAsync(CacheKeys.Summary(user!.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboard_ShouldWriteToRedis_WhenRedisAvailable()
    {
        var token = await SetupUserAsync("redis-dashboard@example.com");
        await SetupDataAsync(token);
        var user = await TestDb.FindUserAsync(_fixture.Factory, "redis-dashboard@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _fixture.KeyExistsAsync(CacheKeys.Dashboard(user!.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task CreateTransaction_ShouldInvalidateCacheInRedis_WhenRedisAvailable()
    {
        var token = await SetupUserAsync("redis-invalidate@example.com");
        var extraTransaction = await SetupDataAsync(token);
        var user = await TestDb.FindUserAsync(_fixture.Factory, "redis-invalidate@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        await _client.GetAsync(ApiRoutes.Dashboard.Base);

        var summaryKey = CacheKeys.Summary(user!.Id);
        var dashboardKey = CacheKeys.Dashboard(user.Id);
        (await _fixture.KeyExistsAsync(summaryKey)).Should().BeTrue();
        (await _fixture.KeyExistsAsync(dashboardKey)).Should().BeTrue();

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, extraTransaction);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        (await _fixture.KeyExistsAsync(summaryKey)).Should().BeFalse();
        (await _fixture.KeyExistsAsync(dashboardKey)).Should().BeFalse();
    }

    private async Task<string> SetupUserAsync(string email)
    {
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1!", null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1!"));
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private async Task<CreateTransactionRequest> SetupDataAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var accountResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Main Account", 1000m, Currency.USD));
        var account = await accountResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var incomeCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Salary", TransactionType.Income));
        var incomeCat = await incomeCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var rentCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Rent", TransactionType.Expense));
        var rentCat = await rentCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var groceriesCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Groceries", TransactionType.Expense));
        var groceriesCat = await groceriesCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account!.Id, incomeCat!.Id, 5000m, TransactionType.Income, "Monthly salary", DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, rentCat.Id, 1500m, TransactionType.Expense, "Monthly rent", DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, groceriesCat!.Id, 200m, TransactionType.Expense, "Groceries", DateTime.UtcNow));

        return new CreateTransactionRequest(account.Id, rentCat.Id, 500m, TransactionType.Expense, "Additional rent", DateTime.UtcNow);
    }
}