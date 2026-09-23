using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.PasswordReset;
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
    private const string GenericResponse = "If this email belongs to an account, a password reset code has been sent.";

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

    private string LatestResetCode(string email) =>
        EmailSender.LatestCode(email, "Reset your password");

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

        var code = LatestResetCode("pr-existing@example.com");
        code.Should().MatchRegex("^[0-9]{6}$");

        var user = await TestDb.FindUserAsync(factory, "pr-existing@example.com");
        user!.PasswordResetCodeHash.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetCodeExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        user.PasswordResetAttemptCount.Should().Be(0);
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

        EmailSender.SentEmails.Should().NotContain(m => m.To == "pr-unknown@example.com");
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
    public async Task ResetPassword_ShouldChangePassword_WhenCodeIsValid()
    {
        await RegisterAsync("pr-reset@example.com");
        await RequestResetAsync("pr-reset@example.com");

        var code = LatestResetCode("pr-reset@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-reset@example.com", code, "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newLogin = await LoginAsync("pr-reset@example.com", "NewPassword1!");
        newLogin.AccessToken.Should().NotBeNullOrWhiteSpace();

        var oldLogin = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest("pr-reset@example.com", "Password1!"));
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var user = await TestDb.FindUserAsync(factory, "pr-reset@example.com");
        user!.PasswordResetCodeHash.Should().BeNull();
        user.PasswordResetCodeExpiresAtUtc.Should().BeNull();
        user.PasswordResetAttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenCodeIsInvalid_AndIncrementAttempts()
    {
        await RegisterAsync("pr-invalid@example.com");
        await RequestResetAsync("pr-invalid@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-invalid@example.com", "000000", "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await TestDb.FindUserAsync(factory, "pr-invalid@example.com");
        user!.PasswordResetCodeHash.Should().NotBeNull();
        user.PasswordResetAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ResetPassword_ShouldLockAfterMaxFailedAttempts()
    {
        await RegisterAsync("pr-lock@example.com");
        await RequestResetAsync("pr-lock@example.com");

        for (var i = 1; i <= 5; i++)
        {
            var response = await _client.PostAsJsonAsync(
                ApiRoutes.Auth.ResetPassword,
                new ResetPasswordRequest("pr-lock@example.com", "000000", "NewPassword1!"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var user = await TestDb.FindUserAsync(factory, "pr-lock@example.com");
        user!.PasswordResetCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenCodeIsExpired()
    {
        await RegisterAsync("pr-expired@example.com");
        await RequestResetAsync("pr-expired@example.com");

        var user = await TestDb.FindUserAsync(factory, "pr-expired@example.com");

        await TestDb.ExpirePasswordResetAsync(factory, user!.Id);

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-expired@example.com", "123456", "NewPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = await TestDb.FindUserAsync(factory, "pr-expired@example.com");
        after!.PasswordResetCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenCodeIsReused()
    {
        await RegisterAsync("pr-reuse@example.com");
        await RequestResetAsync("pr-reuse@example.com");

        var code = LatestResetCode("pr-reuse@example.com");

        var first = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-reuse@example.com", code, "NewPassword1!"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-reuse@example.com", code, "AnotherPassword1!"));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenNewPasswordIsTooWeak()
    {
        await RegisterAsync("pr-weak@example.com");
        await RequestResetAsync("pr-weak@example.com");

        var code = LatestResetCode("pr-weak@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-weak@example.com", code, "weak"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShouldRevokeExistingRefreshTokens()
    {
        await RegisterAsync("pr-revoke@example.com");
        var loginContent = await LoginAsync("pr-revoke@example.com", "Password1!");

        await RequestResetAsync("pr-revoke@example.com");
        var code = LatestResetCode("pr-revoke@example.com");

        var reset = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResetPassword,
            new ResetPasswordRequest("pr-revoke@example.com", code, "NewPassword1!"));
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Refresh,
            new Features.Authentication.Refresh.RefreshRequest(loginContent.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed record GenericMessage(string Message);