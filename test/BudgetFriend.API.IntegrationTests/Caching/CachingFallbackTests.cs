using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Dashboards.GetSummary;
using BudgetFriend.API.Features.Transactions.Create;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Caching;

[Collection("IntegrationTests")]
public sealed class CachingFallbackTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly ICacheService _cache = factory.Services.GetRequiredService<ICacheService>();

    [Fact]
    public async Task SetThenGet_ShouldReturnCachedValue_WhenRedisNotAvailable()
    {
        var key = CacheKeys.Summary(Guid.NewGuid());
        var value = new GetSummaryResponse(
            [new CurrencySummary(Currency.USD, 100m, 50m, 20m, 0m, 0m, 30m)]);

        await _cache.SetAsync(key, value, TimeSpan.FromMinutes(5));

        var cached = await _cache.GetAsync<GetSummaryResponse>(key);

        cached.Should().NotBeNull();
        cached!.Summaries.Should().HaveCount(1);
        cached.Summaries[0].NetAmount.Should().Be(30m);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_ForUnknownKey_WhenRedisNotAvailable()
    {
        var cached = await _cache.GetAsync<GetSummaryResponse>(CacheKeys.Summary(Guid.NewGuid()));

        cached.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteEntry_WhenRedisNotAvailable()
    {
        var key = CacheKeys.Dashboard(Guid.NewGuid());
        var value = new GetSummaryResponse([]);

        await _cache.SetAsync(key, value, TimeSpan.FromMinutes(5));
        await _cache.RemoveAsync(key);

        (await _cache.GetAsync<GetSummaryResponse>(key)).Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldDeleteOnlyMatchingKeys_WhenRedisNotAvailable()
    {
        var userId = Guid.NewGuid();
        var summaryKey = CacheKeys.Summary(userId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var summaryDefaultKey = CacheKeys.Summary(userId);
        var dashboardKey = CacheKeys.Dashboard(userId);
        var value = new GetSummaryResponse([]);

        await _cache.SetAsync(summaryKey, value, TimeSpan.FromMinutes(5));
        await _cache.SetAsync(summaryDefaultKey, value, TimeSpan.FromMinutes(5));
        await _cache.SetAsync(dashboardKey, value, TimeSpan.FromMinutes(5));

        await _cache.RemoveByPrefixAsync(CacheKeys.SummaryPrefix(userId));

        (await _cache.GetAsync<GetSummaryResponse>(summaryKey)).Should().BeNull();
        (await _cache.GetAsync<GetSummaryResponse>(summaryDefaultKey)).Should().BeNull();
        (await _cache.GetAsync<GetSummaryResponse>(dashboardKey)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummary_ShouldBeServedFromInMemoryCache_AndInvalidatedAfterTransaction()
    {
        var token = await SetupUserAsync("memory-invalidate@example.com");
        var extraTransaction = await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var first = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var firstContent = await first.Content.ReadFromJsonAsync<GetSummaryResponse>();

        first.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        firstContent!.Summaries.Should().HaveCount(1);
        firstContent.Summaries[0].TotalExpenses.Should().Be(1700m);

        var user = await TestDb.FindUserAsync(factory, "memory-invalidate@example.com");
        var memoryKey = "bf-cache:" + CacheKeys.Summary(user!.Id, null, null);
        using (var scope = factory.Services.CreateScope())
        {
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var cachedInMemory = memoryCache.TryGetValue(memoryKey, out _);
            cachedInMemory.Should().BeTrue();
        }

        var second = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var secondContent = await second.Content.ReadFromJsonAsync<GetSummaryResponse>();
        secondContent!.Summaries[0].TotalExpenses.Should().Be(1700m);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, extraTransaction);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var third = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var thirdContent = await third.Content.ReadFromJsonAsync<GetSummaryResponse>();

        thirdContent!.Summaries[0].TotalExpenses.Should().Be(2200m);
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