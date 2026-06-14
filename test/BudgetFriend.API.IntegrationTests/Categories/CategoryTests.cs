using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Categories;
using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Categories.Update;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Categories;

[Collection("IntegrationTests")]
public sealed class CategoryTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetTokenAsync(string email, string password)
    {
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, password, null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private void SetAuthHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn201_WhenValidRequest()
    {
        var token = await GetTokenAsync("create-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateCategoryRequest("Groceries", TransactionType.Expense);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnCreatedResponse_WithCorrectData()
    {
        var token = await GetTokenAsync("create-cat-data@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateCategoryRequest("Salary", TransactionType.Income);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, request);
        var content = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        content.Should().NotBeNull();
        content!.Name.Should().Be("Salary");
        content.TransactionType.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn409_WhenDuplicateNameAndType()
    {
        var token = await GetTokenAsync("dup-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var request = new CreateCategoryRequest("Rent", TransactionType.Expense);
        await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, request);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCategory_ShouldAllowSameName_DifferentType()
    {
        var token = await GetTokenAsync("same-name-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var expense = new CreateCategoryRequest("Transfer", TransactionType.Expense);
        var income = new CreateCategoryRequest("Transfer", TransactionType.Income);

        var response1 = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, expense);
        var response2 = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, income);

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new CreateCategoryRequest("Test", TransactionType.Expense);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400_WhenInvalidTransactionType()
    {
        var token = await GetTokenAsync("invalid-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var payload = new { Name = "Invalid", TransactionType = 99 };

        var response = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllCategories_ShouldReturnEmptyList_WhenNoCategories()
    {
        var token = await GetTokenAsync("empty-cats@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.GetAsync(ApiRoutes.Categories.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetCategoryResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCategories_ShouldReturnCategories_WhenTheyExist()
    {
        var token = await GetTokenAsync("list-cats@example.com", "Password1!");
        SetAuthHeader(token);

        await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Food", TransactionType.Expense));
        await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Freelance", TransactionType.Income));

        var response = await _client.GetAsync(ApiRoutes.Categories.Base);
        var content = await response.Content.ReadFromJsonAsync<List<GetCategoryResponse>>();

        content.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnCategory_WhenItExists()
    {
        var token = await GetTokenAsync("get-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Utilities", TransactionType.Expense));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var response = await _client.GetAsync(ApiRoutes.Categories.ById(created!.Id));
        var content = await response.Content.ReadFromJsonAsync<GetCategoryResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Name.Should().Be("Utilities");
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("get-cat-404@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.GetAsync(ApiRoutes.Categories.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn200_WhenValid()
    {
        var token = await GetTokenAsync("update-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("Old Cat", TransactionType.Expense));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var updateRequest = new UpdateCategoryRequest("New Cat", TransactionType.Income);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Categories.ById(created!.Id), updateRequest);
        var content = await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content!.Name.Should().Be("New Cat");
        content.TransactionType.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("update-cat-404@example.com", "Password1!");
        SetAuthHeader(token);

        var updateRequest = new UpdateCategoryRequest("Any", TransactionType.Expense);
        var response = await _client.PutAsJsonAsync(ApiRoutes.Categories.ById(Guid.NewGuid()), updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn204_WhenItExists()
    {
        var token = await GetTokenAsync("delete-cat@example.com", "Password1!");
        SetAuthHeader(token);

        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Categories.Base, new CreateCategoryRequest("To Delete", TransactionType.Expense));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        var response = await _client.DeleteAsync(ApiRoutes.Categories.ById(created!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn404_WhenNotFound()
    {
        var token = await GetTokenAsync("delete-cat-404@example.com", "Password1!");
        SetAuthHeader(token);

        var response = await _client.DeleteAsync(ApiRoutes.Categories.ById(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
