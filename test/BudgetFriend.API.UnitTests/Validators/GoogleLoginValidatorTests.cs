using BudgetFriend.API.Features.Authentication.Google;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class GoogleLoginValidatorTests
{
    private readonly GoogleLoginValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenIdTokenIsProvided()
    {
        var request = new GoogleLoginRequest("valid-id-token");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenIdTokenIsEmpty()
    {
        var request = new GoogleLoginRequest("");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenIdTokenIsNull()
    {
        var request = new GoogleLoginRequest(null!);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenIdTokenExceedsMaxLength()
    {
        var request = new GoogleLoginRequest(new string('a', 8193));

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }
}