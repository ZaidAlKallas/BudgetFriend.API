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
    public async Task Register_ShouldCreateUnverifiedUser_AndSendSixDigitCode()
    {
        await RegisterAsync("ev-create@example.com");

        var user = await TestDb.FindUserAsync(factory, "ev-create@example.com");

        user.Should().NotBeNull();
        user!.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationCodeHash.Should().NotBeNullOrWhiteSpace();
        user.EmailVerificationCodeExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        user.EmailVerificationAttemptCount.Should().Be(0);

        var email = EmailSender.SentEmails.Single(m => m.To == "ev-create@example.com");
        TestEmailExtensions.ExtractCode(email.HtmlBody).Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn200AndMarkVerified_WhenCodeIsValid()
    {
        await RegisterAsync("ev-valid@example.com");
        var code = EmailSender.LatestCode("ev-valid@example.com", "Verify your email");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-valid@example.com", code));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await TestDb.FindUserAsync(factory, "ev-valid@example.com");
        user!.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAtUtc.Should().NotBeNull();
        user.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenCodeIsInvalid_AndIncrementAttempts()
    {
        await RegisterAsync("ev-invalid@example.com");

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-invalid@example.com", "000000"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await TestDb.FindUserAsync(factory, "ev-invalid@example.com");
        user!.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationCodeHash.Should().NotBeNull();
        user.EmailVerificationAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenCodeIsExpired()
    {
        await RegisterAsync("ev-expired@example.com");
        var user = await TestDb.FindUserAsync(factory, "ev-expired@example.com");

        await TestDb.ExpireEmailVerificationAsync(factory, user!.Id);

        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-expired@example.com", "123456"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = await TestDb.FindUserAsync(factory, "ev-expired@example.com");
        after!.IsEmailVerified.Should().BeFalse();
        after.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_ShouldLockAccount_AfterMaxFailedAttempts()
    {
        await RegisterAsync("ev-lock@example.com");
        var code = EmailSender.LatestCode("ev-lock@example.com", "Verify your email");

        for (var i = 1; i <= 5; i++)
        {
            var response = await _client.PostAsJsonAsync(
                ApiRoutes.Auth.VerifyEmail,
                new VerifyEmailRequest("ev-lock@example.com", "000000"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var user = await TestDb.FindUserAsync(factory, "ev-lock@example.com");
        user!.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationCodeHash.Should().BeNull();

        var afterLock = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-lock@example.com", code));
        afterLock.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturn400_WhenCodeIsReused()
    {
        await RegisterAsync("ev-reuse@example.com");
        var code = EmailSender.LatestCode("ev-reuse@example.com", "Verify your email");

        var first = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-reuse@example.com", code));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-reuse@example.com", code));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await TestDb.FindUserAsync(factory, "ev-reuse@example.com");
        user!.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task ResendVerification_ShouldSendNewCodeAndInvalidateOldOne()
    {
        await RegisterAsync("ev-resend@example.com");
        var initialCode = EmailSender.LatestCode("ev-resend@example.com", "Verify your email");

        var resend = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.ResendVerification,
            new ResendVerificationRequest("ev-resend@example.com"));

        resend.StatusCode.Should().Be(HttpStatusCode.OK);

        var newCode = EmailSender.LatestCode("ev-resend@example.com", "Verify your email");
        newCode.Should().NotBe(initialCode);

        var oldCodeResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-resend@example.com", initialCode));
        oldCodeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newCodeResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-resend@example.com", newCode));
        newCodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
        var code = EmailSender.LatestCode("ev-verified@example.com", "Verify your email");

        var verify = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.VerifyEmail,
            new VerifyEmailRequest("ev-verified@example.com", code));
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