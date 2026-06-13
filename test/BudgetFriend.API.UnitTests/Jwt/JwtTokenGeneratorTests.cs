using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BudgetFriend.API.Database.Entites;
using Microsoft.IdentityModel.Tokens;
using BudgetFriend.API.Features.Authentication.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BudgetFriend.API.UnitTests.Jwt;

public sealed class JwtTokenGeneratorTests
{
    private readonly JwtOptions _jwtOptions = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SecretKey = "this-is-a-test-secret-key-that-is-32-characters!",
        ExpirationMinutes = 60
    };

    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_jwtOptions);
        _sut = new JwtTokenGenerator(optionsMock.Object);
    }

    [Fact]
    public void Generate_ShouldReturnValidJwtToken_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(token);
        canRead.Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldContainSubClaim_WhenUserIsValid()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Subject.Should().Be(userId.ToString());
    }

    [Fact]
    public void Generate_ShouldContainEmailClaim_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void Generate_ShouldSetCorrectIssuer_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be(_jwtOptions.Issuer);
    }

    [Fact]
    public void Generate_ShouldSetCorrectAudience_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Audiences.Should().Contain(_jwtOptions.Audience);
    }

    [Fact]
    public void Generate_ShouldSetExpiration_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Generate_ShouldUseHmacSha256_WhenUserIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var token = _sut.Generate(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.SignatureAlgorithm.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void Generate_ShouldThrow_WhenSecretKeyIsEmpty()
    {
        var options = new JwtOptions
        {
            Issuer = "Test",
            Audience = "Test",
            SecretKey = "",
            ExpirationMinutes = 60
        };
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        var generator = new JwtTokenGenerator(optionsMock.Object);
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        var act = () => generator.Generate(user);

        act.Should().Throw<ArgumentException>();
    }
}
