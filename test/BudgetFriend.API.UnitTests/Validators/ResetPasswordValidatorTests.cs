using BudgetFriend.API.Features.Authentication.PasswordReset;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenTokenAndPasswordAreValid()
    {
        var request = new ResetPasswordRequest("valid-token", "Password1!");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTokenIsEmpty()
    {
        var request = new ResetPasswordRequest("", "Password1!");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        var request = new ResetPasswordRequest("valid-token", "");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        var request = new ResetPasswordRequest("valid-token", "Ab1!c");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoSpecialCharacter()
    {
        var request = new ResetPasswordRequest("valid-token", "Password1");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordExceedsMaxLength()
    {
        var request = new ResetPasswordRequest("valid-token", "Password1!" + new string('x', 120));

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}