using System.Net;
using System.Net.Http.Json;
using BudgetFriend.API.Features.Accounts;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Accounts.Update;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;

namespace BudgetFriend.API.IntegrationTests.Accounts;

[Collection("IntegrationTests")]
public sealed class AccountTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetTokenAsync(string email, string password)
    {
        var register = new RegisterRequest(email, password, null, null);
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, register);

        var login = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, login);
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private void SetAuthHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    [Fact]
    public async Task CreateAccount_ShouldReturn201_WhenValidRequest()
    {
        var token = await GetTokenAsync("create-account@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateAccountRequest("Checking Account", 1000m);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAccount_ShouldReturnCreatedResponse_WithCorrectData()
    {
        var token = await GetTokenAsync("create-account-data@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateAccountRequest("Savings", 5000m);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);
        var content = await response.Content.ReadFromJsonAsync<CreateAccountResponse>();

        content.Should().NotBeNull();
        content!.Name.Should().Be("Savings");
        content.InitialBalance.Should().Be(5000m);
        content.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAccount_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new CreateAccountRequest("Unauthorized", 100m);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_ShouldReturn400_WhenNameIsEmpty()
    {
        var token = await GetTokenAsync("create-account-empty@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateAccountRequest("", 100m);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAccount_ShouldReturn409_WhenDuplicateName()
    {
        var token = await GetTokenAsync("duplicate-account@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateAccountRequest("Unique Name", 100m);
        await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAllAccounts_ShouldReturnEmptyList_WhenNoAccounts()
    {
        var token = await GetTokenAsync("empty-account-list@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.GetAsync(ApiRoutes.Accounts.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetAccountResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAccounts_ShouldReturnAccounts_WhenTheyExist()
    {
        var token = await GetTokenAsync("list-accounts@example.com", "Password1!");
        SetAuthHeader(token);

        await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Account A", 100m));
        await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Account B", 200m));

        var response = await _client.GetAsync(ApiRoutes.Accounts.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetAccountResponse>>();

        content.Should().HaveCount(2);
        content.Should().Contain(a => a.Name == "Account A");
        content.Should().Contain(a => a.Name == "Account B");
    }

    [Fact]
    public async Task GetAccountById_ShouldReturnAccount_WhenItExists()
    {
        var token = await GetTokenAsync("get-account-byid@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Target Account", 300m));
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var response = await _client.GetAsync(ApiRoutes.Accounts.ById(createContent!.Id));
        var content = await response.Content.ReadFromJsonAsync<GetAccountResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Name.Should().Be("Target Account");
        content.InitialBalance.Should().Be(300m);
    }

    [Fact]
    public async Task GetAccountById_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("get-account-404@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.GetAsync(ApiRoutes.Accounts.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAccount_ShouldReturn200_WhenValid()
    {
        var token = await GetTokenAsync("update-account@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("Old Name", 100m));
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var updateRequest = new UpdateAccountRequest("New Name", 500m);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Accounts.ById(createContent!.Id), updateRequest);
        var content = await response.Content.ReadFromJsonAsync<UpdateAccountResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Name.Should().Be("New Name");
        content.InitialBalance.Should().Be(500m);
    }

    [Fact]
    public async Task UpdateAccount_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("update-account-404@example.com", "Password1!");
        SetAuthHeader(token);

        var updateRequest = new UpdateAccountRequest("Any Name", 100m);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Accounts.ById(Guid.NewGuid()), updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturn204_WhenItExists()
    {
        var token = await GetTokenAsync("delete-account@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest("To Delete", 100m));
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();

        var response = await _client.DeleteAsync(ApiRoutes.Accounts.ById(createContent!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("delete-account-404@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.DeleteAsync(ApiRoutes.Accounts.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
