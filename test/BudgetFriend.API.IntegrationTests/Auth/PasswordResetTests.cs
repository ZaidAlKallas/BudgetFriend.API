using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.PasswordReset;
using BudgetFriend.API.Features.Authentication.Refresh;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public sealed class PasswordResetTests(BudgetFriendApiFactory factory)
{
    private const string GenericResponse = "If this email belongs to an account, a password reset link has been sent.";

    private readonly HttpClient _client = factory.CreateClient();

    private TestEmailSender EmailSender =>
        (TestEmailSender)factory.Services.GetRequiredService<IEmailSender>();

    private async Task RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task RequestResetAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ForgotPassword,
            new ForgotPasswordRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnGenericResponse_AndSendEmail_WhenAccountExists()
    {
        await RegisterAsync("pr-existing@example.com");
        await RequestResetAsync("pr-existing@example.com");

        var sender = EmailSender;
        var token = sender.LatestToken("pr-existing@example.com", "reset-password");
        token.Should().NotBeNullOrWhiteSpace();

        var user = await TestDb.FindUserAsync(factory, "pr-existing@example.com");
        user!.PasswordResetTokenHash.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnGenericResponse_WhenAccountDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ForgotPassword,
            new ForgotPasswordRequest("pr-unknown@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GenericMessage>();
        content!.Message.Should().Be(GenericResponse);

        var sender = EmailSender;
        sender.SentEmails.Should().NotContain(m => m.To == "pr-unknown@example.com");
    }

    [Fact]
    public async Task ForgotPassword_ShouldUseSameGenericMessage_ForExistingAndUnknownEmails()
    {
        await RegisterAsync("pr-same@example.com");
        await RequestResetAsync("pr-same@example.com");

        var existingResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ForgotPassword,
            new ForgotPasswordRequest("pr-same@example.com"));
        var existing = await existingResponse.Content.ReadFromJsonAsync<GenericMessage>();

        var unknownResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ForgotPassword,
            new ForgotPasswordRequest("pr-does-not-exist@example.com"));
        var unknown = await unknownResponse.Content.ReadFromJsonAsync<GenericMessage>();

        existing!.Message.Should().Be(unknown!.Message);
    }

    [Fact]
    public async Task ResetPassword_ShouldChangePassword_WhenTokenIsValid()
    {
        await RegisterAsync("pr-reset@example.com");
        await RequestResetAsync("pr-reset@example.com");

        var sender = EmailSender;
        var token = sender.LatestToken("pr-reset@example.com", "reset-password");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newLogin = await LoginAsync("pr-reset@example.com", "NewPassword1!");
        newLogin.AccessToken.Should().NotBeNullOrWhiteSpace();

        var oldLogin = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest("pr-reset@example.com", "Password1!"));
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await TestDb.FindUserAsync(factory, "pr-reset@example.com");
        user!.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenTokenIsInvalid()
    {
        await RegisterAsync("pr-invalid@example.com");
        await RequestResetAsync("pr-invalid@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("not-a-real-token", "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenTokenIsExpired()
    {
        await RegisterAsync("pr-expired@example.com");
        await RequestResetAsync("pr-expired@example.com");

        var sender = EmailSender;
        var token = sender.LatestToken("pr-expired@example.com", "reset-password");
        var user = await TestDb.FindUserAsync(factory, "pr-expired@example.com");

        await TestDb.ExpirePasswordResetAsync(factory, user!.Id);

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = await TestDb.FindUserAsync(factory, "pr-expired@example.com");
        after!.PasswordResetTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenTokenIsReused()
    {
        await RegisterAsync("pr-reuse@example.com");
        await RequestResetAsync("pr-reuse@example.com");

        var sender = EmailSender;
        var token = sender.LatestToken("pr-reuse@example.com", "reset-password");

        var first = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "NewPassword1!"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "AnotherPassword1!"));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenNewPasswordIsTooWeak()
    {
        await RegisterAsync("pr-weak@example.com");
        await RequestResetAsync("pr-weak@example.com");

        var sender = EmailSender;
        var token = sender.LatestToken("pr-weak@example.com", "reset-password");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "weak"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldRevokeExistingRefreshTokens()
    {
        await RegisterAsync("pr-revoke@example.com");
        var loginContent = await LoginAsync("pr-revoke@example.com", "Password1!");

        await RequestResetAsync("pr-revoke@example.com");
        var sender = EmailSender;
        var token = sender.LatestToken("pr-revoke@example.com", "reset-password");

        var reset = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest(token, "NewPassword1!"));
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Refresh,
            new Features.Authentication.Refresh.RefreshRequest(loginContent.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed record GenericMessage(string Message);
