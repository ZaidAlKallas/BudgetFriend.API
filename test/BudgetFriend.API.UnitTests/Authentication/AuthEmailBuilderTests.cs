using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Email;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.UnitTests.Authentication;

public sealed class AuthEmailBuilderTests
{
    private static IOptions<EmailOptions> BuildOptions(string? deepLinkBaseUrl, string baseUrl = "https://web.test.local") =>
        Options.Create(new EmailOptions
        {
            BaseUrl = baseUrl,
            DeepLinkBaseUrl = deepLinkBaseUrl
        });

    [Fact]
    public void BuildVerificationCodeMessage_ShouldContainTheCode()
    {
        var message = AuthEmailBuilder.BuildVerificationCodeMessage("123456");

        message.Should().Contain("<strong>123456</strong>");
    }

    [Fact]
    public void BuildVerificationCodeMessage_ShouldNotContainAnyLink()
    {
        var message = AuthEmailBuilder.BuildVerificationCodeMessage("123456");

        message.Should().NotContain("href=");
        message.Should().NotContain("http");
    }

    [Fact]
    public void BuildPasswordResetMessage_ShouldPreferDeepLinkBaseUrl_WhenConfigured()
    {
        var options = BuildOptions("https://app.test.local");

        var message = AuthEmailBuilder.BuildPasswordResetMessage(options, "reset-token");

        message.Should().Contain("https://app.test.local/reset-password?token=reset-token");
        message.Should().NotContain("https://web.test.local");
    }

    [Fact]
    public void BuildPasswordResetMessage_ShouldFallBackToBaseUrl_WhenDeepLinkIsNotConfigured()
    {
        var options = BuildOptions(null);

        var message = AuthEmailBuilder.BuildPasswordResetMessage(options, "reset-token");

        message.Should().Contain("https://web.test.local/reset-password?token=reset-token");
    }

    [Fact]
    public void BuildPasswordResetMessage_ShouldUrlEncodeToken()
    {
        var options = BuildOptions("https://app.test.local");

        var message = AuthEmailBuilder.BuildPasswordResetMessage(options, "a b/c+d=");

        message.Should().Contain("https://app.test.local/reset-password?token=a%20b%2Fc%2Bd%3D");
    }
}