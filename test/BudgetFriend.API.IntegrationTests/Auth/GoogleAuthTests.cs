using BudgetFriend.API.Features.Authentication.Google;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public sealed class GoogleAuthTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static void SetGoogleUser(string subject, string? email, bool emailVerified, string? firstName = null, string? lastName = null)
        => FakeGoogleIdTokenValidator.Result = new GoogleUserInfo(subject, email, emailVerified, firstName, lastName);

    [Fact]
    public async Task GoogleLogin_ShouldCreateUser_WhenNoMatchingAccountExists()
    {
        SetGoogleUser("google-subject-1", "google-new@example.com", true, "Google", "User");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var user = await TestDb.FindUserAsync(factory, "google-new@example.com");
        user.Should().NotBeNull();
        user!.GoogleSubject.Should().Be("google-subject-1");
        user.IsEmailVerified.Should().BeTrue();
        user.FirstName.Should().Be("Google");
        user.LastName.Should().Be("User");
        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldIssueTokenThatWorksOnProtectedEndpoints()
    {
        SetGoogleUser("google-subject-2", "google-profile@example.com", true);

        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Auth.Profile)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.AccessToken) }
        };

        var profileResponse = await _client.SendAsync(request);

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnExistingUser_WhenSubjectIsAlreadyLinked()
    {
        SetGoogleUser("google-subject-3", "google-existing@example.com", true);

        var first = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        SetGoogleUser("google-subject-3", "google-existing@example.com", true);

        var second = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await TestDb.FindUserAsync(factory, "google-existing@example.com");
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldNotCreateDuplicate_WhenSubjectIsAlreadyLinkedWithDifferentEmail()
    {
        SetGoogleUser("google-subject-4", "google-dup@example.com", true);

        var first = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        SetGoogleUser("google-subject-4", "a-different-email@example.com", true);

        var second = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var original = await TestDb.FindUserAsync(factory, "google-dup@example.com");
        var other = await TestDb.FindUserAsync(factory, "a-different-email@example.com");
        original.Should().NotBeNull();
        other.Should().BeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldLinkVerifiedGoogleEmail_ToExistingLocalAccount()
    {
        var register = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest("google-link@example.com", "Password1!", "Local", "User"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        SetGoogleUser("google-subject-5", "google-link@example.com", true);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await TestDb.FindUserAsync(factory, "google-link@example.com");
        user.Should().NotBeNull();
        user!.GoogleSubject.Should().Be("google-subject-5");
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturn409_WhenEmailMatchesLocalAccountButGoogleEmailIsUnverified()
    {
        var register = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest("google-unlinked@example.com", "Password1!", null, null));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        SetGoogleUser("google-subject-6", "google-unlinked@example.com", emailVerified: false);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var user = await TestDb.FindUserAsync(factory, "google-unlinked@example.com");
        user!.GoogleSubject.Should().BeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturn409_WhenGoogleAccountHasNoEmail()
    {
        SetGoogleUser("google-subject-7", email: null, emailVerified: true);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("fake-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturn400_WhenIdTokenCannotBeValidated()
    {
        FakeGoogleIdTokenValidator.Result = null;

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("invalid-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturn400_WhenIdTokenIsEmpty()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}