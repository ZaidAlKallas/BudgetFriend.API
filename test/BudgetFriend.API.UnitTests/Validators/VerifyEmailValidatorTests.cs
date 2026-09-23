using BudgetFriend.API.Features.Authentication.EmailVerification;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class VerifyEmailValidatorTests
{
    private readonly VerifyEmailValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenEmailAndSixDigitCodeAreProvided()
    {
        var request = new VerifyEmailRequest("user@example.com", "123456");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = new VerifyEmailRequest("", "123456");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new VerifyEmailRequest("not-an-email", "123456");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsEmpty()
    {
        var request = new VerifyEmailRequest("user@example.com", "");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsNotNumeric()
    {
        var request = new VerifyEmailRequest("user@example.com", "abcdef");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsTooShort()
    {
        var request = new VerifyEmailRequest("user@example.com", "12345");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCodeIsTooLong()
    {
        var request = new VerifyEmailRequest("user@example.com", "1234567");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }
}