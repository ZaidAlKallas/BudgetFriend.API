using BudgetFriend.API.Features.Authentication;
using FluentAssertions;

namespace BudgetFriend.API.UnitTests.Authentication;

public sealed class SecurityTokensTests
{
    [Fact]
    public void Generate_ShouldReturnDistinctValues()
    {
        var first = SecurityTokens.Generate();
        var second = SecurityTokens.Generate();

        first.Should().NotBe(second);
    }

    [Fact]
    public void Generate_ShouldReturnUrlSafeToken()
    {
        var token = SecurityTokens.Generate();

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void Hash_ShouldProduceDeterministic64CharacterHex()
    {
        var hash1 = SecurityTokens.Hash("some-token");
        var hash2 = SecurityTokens.Hash("some-token");

        hash1.Should().Be(hash2);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_ShouldDiffer_ForDifferentTokens()
    {
        SecurityTokens.Hash("token-one").Should().NotBe(SecurityTokens.Hash("token-two"));
    }

    [Fact]
    public void GenerateNumericCode_ShouldReturnSixDigits_ByDefault()
    {
        var code = SecurityTokens.GenerateNumericCode();

        code.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public void GenerateNumericCode_ShouldSupportCustomLength()
    {
        var code = SecurityTokens.GenerateNumericCode(4);

        code.Should().MatchRegex("^[0-9]{4}$");
    }

    [Fact]
    public void GenerateNumericCode_ShouldReturnDistinctValues()
    {
        var values = Enumerable.Range(0, 100)
            .Select(_ => SecurityTokens.GenerateNumericCode())
            .Distinct()
            .ToList();

        values.Should().HaveCountGreaterThan(20);
    }
}