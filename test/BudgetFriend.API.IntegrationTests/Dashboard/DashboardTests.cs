using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Dashboards.GetDashboard;
using BudgetFriend.API.Features.Dashboards.GetSummary;
using BudgetFriend.API.Features.Transactions.Create;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Dashboard;

[Collection("IntegrationTests")]
public sealed class DashboardTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> SetupUserAsync(string email)
    {
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1!", null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1!"));
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private async Task<(Guid AccountId, Guid IncomeCategoryId, Guid RentCategoryId, Guid GroceriesCategoryId)> SetupDataAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var accountResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Main Account", 1000m));
        var account = await accountResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var incomeCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Salary", TransactionType.Income));
        var incomeCat = await incomeCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var rentCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Rent", TransactionType.Expense));
        var rentCat = await rentCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var groceriesCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Groceries", TransactionType.Expense));
        var groceriesCat = await groceriesCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account!.Id, incomeCat!.Id, 5000m, "Monthly salary", DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, rentCat!.Id, 1500m, "Monthly rent", DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, groceriesCat!.Id, 200m, "Groceries", DateTime.UtcNow));

        return (account.Id, incomeCat.Id, rentCat.Id, groceriesCat.Id);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn200_WhenAuthenticated()
    {
        var token = await SetupUserAsync("dash-auth@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnCorrectBalances_WhenDataExists()
    {
        var token = await SetupUserAsync("dash-balance@example.com");
        await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);
        var content = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        content.Should().NotBeNull();
        content!.TotalBalance.Should().Be(4300m);
        content.MonthlyIncome.Should().Be(5000m);
        content.MonthlyExpenses.Should().Be(1700m);
        content.NetMonthlyIncome.Should().Be(3300m);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnAccountSummaries_WhenDataExists()
    {
        var token = await SetupUserAsync("dash-accounts@example.com");
        await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);
        var content = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        content!.Accounts.Should().HaveCount(1);
        content.Accounts[0].Name.Should().Be("Main Account");
        content.Accounts[0].Balance.Should().Be(4300m);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnTopExpenseCategories_WhenDataExists()
    {
        var token = await SetupUserAsync("dash-top-expenses@example.com");
        await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);
        var content = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        content!.TopExpenseCategories.Should().NotBeEmpty();
        content.TopExpenseCategories[0].CategoryName.Should().Be("Rent");
        content.TopExpenseCategories[0].TotalAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnRecentTransactions_WhenDataExists()
    {
        var token = await SetupUserAsync("dash-recent@example.com");
        await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);
        var content = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        content!.RecentTransactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnZeroValues_WhenNoData()
    {
        var token = await SetupUserAsync("dash-empty@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Base);
        var content = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        content!.TotalBalance.Should().Be(0m);
        content.MonthlyIncome.Should().Be(0m);
        content.MonthlyExpenses.Should().Be(0m);
        content.NetMonthlyIncome.Should().Be(0m);
        content.Accounts.Should().BeEmpty();
        content.TopExpenseCategories.Should().BeEmpty();
        content.RecentTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummary_ShouldReturnCorrectData_WhenDataExists()
    {
        var token = await SetupUserAsync("summary-data@example.com");
        await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var content = await response.Content.ReadFromJsonAsync<GetSummaryResponse>();

        content.Should().NotBeNull();
        content!.TotalIncome.Should().Be(5000m);
        content.TotalExpenses.Should().Be(1700m);
        content.NetAmount.Should().Be(3300m);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnCategoryBreakdown_WithPercentages()
    {
        var token = await SetupUserAsync("summary-breakdown@example.com");
        var (_, _, rentCatId, groceriesCatId) = await SetupDataAsync(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var content = await response.Content.ReadFromJsonAsync<GetSummaryResponse>();

        content!.CategoryBreakdown.Should().HaveCount(3);

        var rentBreakdown = content.CategoryBreakdown.Single(c => c.CategoryId == rentCatId);
        rentBreakdown.Percentage.Should().Be(88.2m);

        var groceriesBreakdown = content.CategoryBreakdown.Single(c => c.CategoryId == groceriesCatId);
        groceriesBreakdown.Percentage.Should().Be(11.8m);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnEmptyBreakdown_WhenNoData()
    {
        var token = await SetupUserAsync("summary-empty@example.com");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Dashboard.Summary);
        var content = await response.Content.ReadFromJsonAsync<GetSummaryResponse>();

        content!.TotalIncome.Should().Be(0m);
        content.TotalExpenses.Should().Be(0m);
        content.NetAmount.Should().Be(0m);
        content.CategoryBreakdown.Should().BeEmpty();
    }
}
