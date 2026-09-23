using BudgetFriend.API.Features.Authentication.PasswordReset;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _sut = new();

    private static readonly ResetPasswordRequest ValidRequest =
        new("user@example.com", "123456", "Password1!");

    [Fact]
    public void Validate_ShouldBeValid_WhenRequestIsValid()
    {
        var result = _sut.TestValidate(ValidRequest);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = ValidRequest with { Email = "" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = ValidRequest with { Email = "not-an-email" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsEmpty()
    {
        var request = ValidRequest with { Code = "" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsNotSixDigits()
    {
        var request = ValidRequest with { Code = "12ab45" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsFiveDigits()
    {
        var request = ValidRequest with { Code = "12345" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        var request = ValidRequest with { NewPassword = "" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        var request = ValidRequest with { NewPassword = "Ab1!c" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoSpecialCharacter()
    {
        var request = ValidRequest with { NewPassword = "Password1" };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordExceedsMaxLength()
    {
        var request = ValidRequest with { NewPassword = "Password1!" + new string('x', 120) };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}