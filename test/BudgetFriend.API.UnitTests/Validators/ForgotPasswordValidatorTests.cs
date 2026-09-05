using BudgetFriend.API.Features.Authentication.PasswordReset;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenEmailIsValid()
    {
        var request = new ForgotPasswordRequest("test@example.com");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = new ForgotPasswordRequest("");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new ForgotPasswordRequest("not-an-email");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailExceedsMaxLength()
    {
        var request = new ForgotPasswordRequest(new string('a', 257) + "@example.com");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}