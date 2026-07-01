using System.Net;
using System.Net.Http.Json;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Accounts;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Transactions;
using BudgetFriend.API.Features.Transactions.Create;
using BudgetFriend.API.Features.Transactions.Update;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;

namespace BudgetFriend.API.IntegrationTests.Transactions;

[Collection("IntegrationTests")]
public sealed class TransactionTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetTokenAsync(string email, string password)
    {
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, password, null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private async Task<CreateAccountResponse> CreateAccountAsync(string token, string name, decimal balance)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest(name, balance, Currency.USD));
        return (await response.Content.ReadFromJsonAsync<CreateAccountResponse>())!;
    }

    private async Task<CreateCategoryResponse> CreateCategoryAsync(string token, string name, TransactionType type)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest(name, type));
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>())!;
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturn201_WhenValidRequest()
    {
        var token = await GetTokenAsync("create-txn@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "Txn Account", 1000m);
        var category = await CreateCategoryAsync(token, "Food", TransactionType.Expense);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransactionRequest(account.Id, category.Id, 50m, "Lunch", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturnCreatedResponse_WithCorrectData()
    {
        var token = await GetTokenAsync("txn-data@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "Txn Data Account", 2000m);
        var category = await CreateCategoryAsync(token, "Salary", TransactionType.Income);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransactionRequest(account.Id, category.Id, 5000m, "Monthly salary", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, request);
        var content = await response.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        content.Should().NotBeNull();
        content!.AccountId.Should().Be(account.Id);
        content.CategoryId.Should().Be(category.Id);
        content.Amount.Should().Be(5000m);
        content.Note.Should().Be("Monthly salary");
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturn404_WhenAccountDoesNotExist()
    {
        var token = await GetTokenAsync("txn-no-account@example.com", "Password1!");
        var category = await CreateCategoryAsync(token, "Misc", TransactionType.Expense);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransactionRequest(Guid.NewGuid(), category.Id, 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var token = await GetTokenAsync("txn-no-cat@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "No Cat Account", 100m);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransactionRequest(account.Id, Guid.NewGuid(), 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTransactions_ShouldReturnEmpty_WhenNoTransactions()
    {
        var token = await GetTokenAsync("empty-txns@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Transactions.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetTransactionResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTransactions_ShouldReturnTransactions_WhenTheyExist()
    {
        var token = await GetTokenAsync("list-txns@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "List Txn Acct", 1000m);
        var incomeCat = await CreateCategoryAsync(token, "Freelance", TransactionType.Income);
        var expenseCat = await CreateCategoryAsync(token, "Coffee", TransactionType.Expense);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, incomeCat.Id, 200m, null, DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, expenseCat.Id, 5m, null, DateTime.UtcNow));

        var response = await _client.GetAsync(ApiRoutes.Transactions.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetTransactionResponse>>();

        content.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionById_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("get-txn-404@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Transactions.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransaction_ShouldReturn200_WhenValid()
    {
        var token = await GetTokenAsync("update-txn@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "Update Txn Acct", 500m);
        var category = await CreateCategoryAsync(token, "Food", TransactionType.Expense);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, category.Id, 30m, "Snack", DateTime.UtcNow));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        var updateRequest = new UpdateTransactionRequest(45m, "Updated snack", DateTime.UtcNow);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Transactions.ById(created!.Id), updateRequest);
        var content = await response.Content.ReadFromJsonAsync<UpdateTransactionResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Amount.Should().Be(45m);
        content.Note.Should().Be("Updated snack");
    }

    [Fact]
    public async Task UpdateTransaction_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("update-txn-404@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var updateRequest = new UpdateTransactionRequest(100m, null, DateTime.UtcNow);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Transactions.ById(Guid.NewGuid()), updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransaction_ShouldReturn204_WhenItExists()
    {
        var token = await GetTokenAsync("delete-txn@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "Del Txn Acct", 300m);
        var category = await CreateCategoryAsync(token, "Gas", TransactionType.Expense);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, category.Id, 40m, null, DateTime.UtcNow));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTransactionResponse>();

        var response = await _client.DeleteAsync(ApiRoutes.Transactions.ById(created!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTransaction_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("delete-txn-404@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.DeleteAsync(ApiRoutes.Transactions.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteWorkflow_RegisterLoginCreateAccountCategoryTransaction_ShouldSucceed()
    {
        var email = $"workflow-{Guid.NewGuid():N}@example.com";
        var password = "Password1!";

        var registerResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, password, "Work", "Flow"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginContent!.AccessToken.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", loginContent.AccessToken);

        var accountResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Main Account", 10000m, Currency.USD));
        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await accountResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var incomeCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Income", TransactionType.Income));
        incomeCatResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var incomeCat = await incomeCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var expenseCatResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Expense", TransactionType.Expense));
        expenseCatResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var expenseCat = await expenseCatResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var incomeTxn = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account!.Id, incomeCat!.Id, 5000m, "Salary", DateTime.UtcNow));
        incomeTxn.StatusCode.Should().Be(HttpStatusCode.Created);

        var expenseTxn = await _client.PostAsJsonAsync(ApiRoutes.Transactions.Base, new CreateTransactionRequest(account.Id, expenseCat!.Id, 200m, "Groceries", DateTime.UtcNow));
        expenseTxn.StatusCode.Should().Be(HttpStatusCode.Created);

        var getTxns = await _client.GetAsync(ApiRoutes.Transactions.Base);
        var txns = await getTxns.Content.ReadFromJsonAsync<List<GetTransactionResponse>>();
        txns.Should().HaveCount(2);
    }
}
