using BudgetFriend.API.Database.Entities;
using BudgetFriend.API.Features.Authentication.EmailVerification;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public sealed class EmailVerificationTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private TestEmailSender EmailSender =>
        (TestEmailSender)factory.Services.GetRequiredService<IEmailSender>();

    private async Task<string> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return email;
    }

    [Fact]
    public async Task Register_ShouldCreateUnverifiedUser_AndSendVerificationEmail()
    {
        await RegisterAsync("ev-create@example.com");

        var user = await TestDb.FindUserAsync(factory, "ev-create@example.com");

        user.Should().NotBeNull();
        user!.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationTokenHash.Should().NotBeNullOrWhiteSpace();
        user.EmailVerificationExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        EmailSender.SentEmails.Should().ContainSingle(m => m.To == "ev-create@example.com");
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn200AndMarkVerified_WhenTokenIsValid()
    {
        await RegisterAsync("ev-valid@example.com");
        var token = EmailSender.LatestToken("ev-valid@example.com", "verify-email");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await TestDb.FindUserAsync(factory, "ev-valid@example.com");
        user!.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAtUtc.Should().NotBeNull();
        user.EmailVerificationTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenTokenIsInvalid()
    {
        await RegisterAsync("ev-invalid@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("not-a-real-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await TestDb.FindUserAsync(factory, "ev-invalid@example.com");
        user!.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenTokenIsExpired()
    {
        await RegisterAsync("ev-expired@example.com");
        var token = EmailSender.LatestToken("ev-expired@example.com", "verify-email");
        var user = await TestDb.FindUserAsync(factory, "ev-expired@example.com");

        await TestDb.ExpireEmailVerificationAsync(factory, user!.Id);

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = await TestDb.FindUserAsync(factory, "ev-expired@example.com");
        after!.IsEmailVerified.Should().BeFalse();
        after.EmailVerificationTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenTokenIsReused()
    {
        await RegisterAsync("ev-reuse@example.com");
        var token = EmailSender.LatestToken("ev-reuse@example.com", "verify-email");

        var first = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyEmail, new VerifyEmailRequest(token));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyEmail, new VerifyEmailRequest(token));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await TestDb.FindUserAsync(factory, "ev-reuse@example.com");
        user!.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task ResendVerification_ShouldSendNewTokenAndInvalidateOldOne()
    {
        await RegisterAsync("ev-resend@example.com");
        var initialToken = EmailSender.LatestToken("ev-resend@example.com", "verify-email");

        var resend = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResendVerification,
            new ResendVerificationRequest("ev-resend@example.com"));

        resend.StatusCode.Should().Be(HttpStatusCode.OK);

        var newToken = EmailSender.LatestToken("ev-resend@example.com", "verify-email");
        newToken.Should().NotBe(initialToken);

        var oldTokenResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyEmail, new VerifyEmailRequest(initialToken));
        oldTokenResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newTokenResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyEmail, new VerifyEmailRequest(newToken));
        newTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_ShouldReturnGenericResponse_WhenEmailDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResendVerification,
            new ResendVerificationRequest("ev-unknown@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EmailSender.SentEmails.Should().NotContain(m => m.To == "ev-unknown@example.com");
    }

    [Fact]
    public async Task ResendVerification_ShouldNotSendEmailAgain_WhenAlreadyVerified()
    {
        await RegisterAsync("ev-verified@example.com");
        var token = EmailSender.LatestToken("ev-verified@example.com", "verify-email");

        var verify = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyEmail, new VerifyEmailRequest(token));
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        var resend = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResendVerification,
            new ResendVerificationRequest("ev-verified@example.com"));

        resend.StatusCode.Should().Be(HttpStatusCode.OK);

        EmailSender.SentEmails
            .Count(m => m.To == "ev-verified@example.com")
            .Should().Be(1);
    }
}