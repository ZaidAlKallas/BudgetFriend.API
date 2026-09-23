using BudgetFriend.API.Features.Authentication;
using FluentAssertions;

namespace BudgetFriend.API.UnitTests.Authentication;

public sealed class AuthEmailBuilderTests
{
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
    public void BuildPasswordResetCodeMessage_ShouldContainTheCode()
    {
        var message = AuthEmailBuilder.BuildPasswordResetCodeMessage("123456");

        message.Should().Contain("<strong>123456</strong>");
    }

    [Fact]
    public void BuildPasswordResetCodeMessage_ShouldNotContainAnyLink()
    {
        var message = AuthEmailBuilder.BuildPasswordResetCodeMessage("123456");

        message.Should().NotContain("href=");
        message.Should().NotContain("http");
    }
}