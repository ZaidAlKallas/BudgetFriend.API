using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public sealed class AuthenticationTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ShouldReturn201_WhenValidRequest()
    {
        var request = new RegisterRequest("newuser@example.com", "Password1!", "John", "Doe");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest("duplicate@example.com", "Password1!", null, null);
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenEmailIsInvalid()
    {
        var request = new RegisterRequest("not-an-email", "Password1!", null, null);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenPasswordIsTooWeak()
    {
        var request = new RegisterRequest("test@example.com", "weak", null, null);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturn200WithToken_WhenCredentialsAreValid()
    {
        var registerRequest = new RegisterRequest("logintest@example.com", "Password1!", null, null);
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, registerRequest);

        var loginRequest = new LoginRequest("logintest@example.com", "Password1!");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();
        content.ExpireAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenEmailDoesNotExist()
    {
        var request = new LoginRequest("nonexistent@example.com", "Password1!");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenPasswordIsWrong()
    {
        var registerRequest = new RegisterRequest("wrongpass@example.com", "Password1!", null, null);
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, registerRequest);

        var loginRequest = new LoginRequest("wrongpass@example.com", "WrongPassword1!");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn400_WhenRequestIsInvalid()
    {
        var request = new LoginRequest("", "");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Profile_ShouldReturn200_WhenAuthenticated()
    {
        var registerRequest = new RegisterRequest("profiletest@example.com", "Password1!", "Jane", "Doe");
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, registerRequest);

        var loginRequest = new LoginRequest("profiletest@example.com", "Password1!");
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Auth.Profile);
        request.Headers.Authorization = new("Bearer", loginContent!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Profile_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync(ApiRoutes.Auth.Profile);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_ShouldReturnCorrectUserData_WhenAuthenticated()
    {
        var registerRequest = new RegisterRequest("profile-data@example.com", "Password1!", "Profile", "Test");
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, registerRequest);

        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest("profile-data@example.com", "Password1!"));
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Auth.Profile);
        request.Headers.Authorization = new("Bearer", loginContent!.AccessToken);

        var response = await _client.SendAsync(request);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        profile.Should().NotBeNull();
        profile!.Email.Should().Be("profile-data@example.com");
        profile.FirstName.Should().Be("Profile");
        profile.LastName.Should().Be("Test");
    }
}

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified,
    DateTime CreatedAtUtc);
