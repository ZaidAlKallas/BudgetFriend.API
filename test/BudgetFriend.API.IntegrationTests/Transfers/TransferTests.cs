using System.Net;
using System.Net.Http.Json;
using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Transfers;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Transfers.Create;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;

namespace BudgetFriend.API.IntegrationTests.Transfers;

[Collection("IntegrationTests")]
public sealed class TransferTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetTokenAsync(string email, string password)
    {
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, password, null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private async Task<CreateAccountResponse> CreateAccountAsync(string token, string name, decimal balance, Currency currency)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base, new CreateAccountRequest(name, balance, currency));
        return (await response.Content.ReadFromJsonAsync<CreateAccountResponse>())!;
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn201_WhenValidRequest()
    {
        var token = await GetTokenAsync("transfer-create@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "From Account", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "To Account", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(fromAccount.Id, toAccount.Id, 100m, 100m, "Test transfer", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturnCreatedResponse_WithCorrectData()
    {
        var token = await GetTokenAsync("transfer-data@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "From Data", 2000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "To Data", 1000m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(fromAccount.Id, toAccount.Id, 250m, 250m, "Monthly transfer", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);
        var content = await response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        content.Should().NotBeNull();
        content!.FromAccountId.Should().Be(fromAccount.Id);
        content.ToAccountId.Should().Be(toAccount.Id);
        content.FromAmount.Should().Be(250m);
        content.ToAmount.Should().Be(250m);
        content.Note.Should().Be("Monthly transfer");
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn404_WhenFromAccountDoesNotExist()
    {
        var token = await GetTokenAsync("transfer-no-from@example.com", "Password1!");
        var toAccount = await CreateAccountAsync(token, "To Only", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(Guid.NewGuid(), toAccount.Id, 100m, 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn404_WhenToAccountDoesNotExist()
    {
        var token = await GetTokenAsync("transfer-no-to@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "From Only", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(fromAccount.Id, Guid.NewGuid(), 100m, 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn400_WhenSameAccounts()
    {
        var token = await GetTokenAsync("transfer-same@example.com", "Password1!");
        var account = await CreateAccountAsync(token, "Same Account", 1000m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(account.Id, account.Id, 100m, 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransfer_ShouldReturn400_WhenAmountIsZero()
    {
        var token = await GetTokenAsync("transfer-zero@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "From Zero", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "To Zero", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(fromAccount.Id, toAccount.Id, 0m, 100m, null, DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMultiCurrencyTransfer_ShouldReturn201_WhenValid()
    {
        var token = await GetTokenAsync("transfer-multi@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "USD Account", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "TRY Account", 10000m, Currency.TRY);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateTransferRequest(fromAccount.Id, toAccount.Id, 100m, 3500m, "USD to TRY", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, request);
        var content = await response.Content.ReadFromJsonAsync<CreateTransferResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        content!.FromAmount.Should().Be(100m);
        content.ToAmount.Should().Be(3500m);
    }

    [Fact]
    public async Task GetAllTransfers_ShouldReturnEmpty_WhenNoTransfers()
    {
        var token = await GetTokenAsync("empty-transfers@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Transfers.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetTransferResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTransfers_ShouldReturnTransfers_WhenTheyExist()
    {
        var token = await GetTokenAsync("list-transfers@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "List From", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "List To", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, new CreateTransferRequest(fromAccount.Id, toAccount.Id, 50m, 50m, null, DateTime.UtcNow));
        await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, new CreateTransferRequest(fromAccount.Id, toAccount.Id, 100m, 100m, null, DateTime.UtcNow));

        var response = await _client.GetAsync(ApiRoutes.Transfers.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetTransferResponse>>();

        content.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransferById_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("get-transfer-404@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync(ApiRoutes.Transfers.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransferById_ShouldReturnTransfer_WhenItExists()
    {
        var token = await GetTokenAsync("get-transfer@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "Get From", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "Get To", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, new CreateTransferRequest(fromAccount.Id, toAccount.Id, 75m, 75m, "Get me", DateTime.UtcNow));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var response = await _client.GetAsync(ApiRoutes.Transfers.ById(created!.Id));
        var content = await response.Content.ReadFromJsonAsync<GetTransferResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Id.Should().Be(created.Id);
        content.Note.Should().Be("Get me");
    }

    [Fact]
    public async Task DeleteTransfer_ShouldReturn204_WhenItExists()
    {
        var token = await GetTokenAsync("delete-transfer@example.com", "Password1!");
        var fromAccount = await CreateAccountAsync(token, "Del From", 1000m, Currency.USD);
        var toAccount = await CreateAccountAsync(token, "Del To", 500m, Currency.USD);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base, new CreateTransferRequest(fromAccount.Id, toAccount.Id, 200m, 200m, null, DateTime.UtcNow));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTransferResponse>();

        var response = await _client.DeleteAsync(ApiRoutes.Transfers.ById(created!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTransfer_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("delete-transfer-404@example.com", "Password1!");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.DeleteAsync(ApiRoutes.Transfers.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteTransferWorkflow_ShouldSucceed()
    {
        var token = await GetTokenAsync("transfer-workflow@example.com", "Password1!");
        var usdAccount = await CreateAccountAsync(token, "USD Main", 5000m, Currency.USD);
        var tryAccount = await CreateAccountAsync(token, "TRY Main", 100000m, Currency.TRY);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Transfers.Base,
            new CreateTransferRequest(usdAccount.Id, tryAccount.Id, 500m, 17500m, "Monthly budget", DateTime.UtcNow));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetAsync(ApiRoutes.Transfers.Base);
        var transfers = await listResponse.Content.ReadFromJsonAsync<List<GetTransferResponse>>();
        transfers.Should().HaveCount(1);
        transfers![0].FromAmount.Should().Be(500m);
        transfers[0].ToAmount.Should().Be(17500m);

        var getResponse = await _client.GetAsync(ApiRoutes.Transfers.ById(transfers[0].Id));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await _client.DeleteAsync(ApiRoutes.Transfers.ById(transfers[0].Id));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDeleteResponse = await _client.GetAsync(ApiRoutes.Transfers.Base);
        var afterDelete = await afterDeleteResponse.Content.ReadFromJsonAsync<List<GetTransferResponse>>();
        afterDelete.Should().BeEmpty();
    }
}
