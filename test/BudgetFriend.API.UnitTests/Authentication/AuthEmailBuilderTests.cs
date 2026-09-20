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
    public void BuildVerificationMessage_ShouldPreferDeepLinkBaseUrl_WhenConfigured()
    {
        var options = BuildOptions("https://app.test.local");

        var message = AuthEmailBuilder.BuildVerificationMessage(options, "token-123");

        message.Should().Contain("https://app.test.local/verify-email?token=token-123");
        message.Should().NotContain("https://web.test.local");
    }

    [Fact]
    public void BuildVerificationMessage_ShouldFallBackToBaseUrl_WhenDeepLinkIsNotConfigured()
    {
        var options = BuildOptions(null);

        var message = AuthEmailBuilder.BuildVerificationMessage(options, "token-123");

        message.Should().Contain("https://web.test.local/verify-email?token=token-123");
    }

    [Fact]
    public void BuildVerificationMessage_ShouldHandleTrailingSlashes_OnDeepLinkBaseUrl()
    {
        var options = BuildOptions("https://app.test.local/");

        var message = AuthEmailBuilder.BuildVerificationMessage(options, "token-123");

        message.Should().Contain("https://app.test.local/verify-email?token=token-123");
        message.Should().NotContain("//verify-email");
    }

    [Fact]
    public void BuildVerificationMessage_ShouldUrlEncodeToken()
    {
        var options = BuildOptions("https://app.test.local");

        var message = AuthEmailBuilder.BuildVerificationMessage(options, "a b/c+d=");

        message.Should().Contain("https://app.test.local/verify-email?token=a%20b%2Fc%2Bd%3D");
    }

    [Fact]
    public void BuildPasswordResetMessage_ShouldPreferDeepLinkBaseUrl_WhenConfigured()
    {
        var options = BuildOptions("https://app.test.local");

        var message = AuthEmailBuilder.BuildPasswordResetMessage(options, "reset-token");

        message.Should().Contain("https://app.test.local/reset-password?token=reset-token");
        message.Should().NotContain("https://web.test.local");
    }
}