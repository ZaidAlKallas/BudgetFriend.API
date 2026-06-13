using System.Security.Claims;
using BudgetFriend.API.Features.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BudgetFriend.API.UnitTests.Authentication;

public sealed class CurrentUserTests
{
    [Fact]
    public void UserId_ShouldReturnCorrectGuid_WhenNameIdentifierClaimIsPresent()
    {
        var userId = Guid.NewGuid();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var sut = new CurrentUser(accessorMock.Object);

        sut.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_ShouldThrow_WhenHttpContextIsNull()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var sut = new CurrentUser(accessorMock.Object);

        var act = () => sut.UserId;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UserId_ShouldThrow_WhenNameIdentifierClaimIsMissing()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var sut = new CurrentUser(accessorMock.Object);

        var act = () => sut.UserId;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UserId_ShouldThrow_WhenNameIdentifierIsNotValidGuid()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        ]));
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var sut = new CurrentUser(accessorMock.Object);

        var act = () => sut.UserId;

        act.Should().Throw<FormatException>();
    }
}
