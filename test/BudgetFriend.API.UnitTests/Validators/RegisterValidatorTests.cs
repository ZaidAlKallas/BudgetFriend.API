using BudgetFriend.API.Features.Authentication.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class RegisterValidatorTests
{
    private readonly RegisterValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", "John", "Doe");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenOptionalFieldsAreNull()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", null, null);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = new RegisterRequest("", "Password1!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new RegisterRequest("not-an-email", "Password1!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailExceedsMaxLength()
    {
        var request = new RegisterRequest(new string('a', 257) + "@example.com", "Password1!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        var request = new RegisterRequest("test@example.com", "", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        var request = new RegisterRequest("test@example.com", "Ab1!c", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoUppercase()
    {
        var request = new RegisterRequest("test@example.com", "password1!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoLowercase()
    {
        var request = new RegisterRequest("test@example.com", "PASSWORD1!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoDigit()
    {
        var request = new RegisterRequest("test@example.com", "Password!!", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordHasNoSpecialCharacter()
    {
        var request = new RegisterRequest("test@example.com", "Password1", null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordExceedsMaxLength()
    {
        var request = new RegisterRequest("test@example.com", "Password1!" + new string('x', 120), null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFirstNameExceedsMaxLength()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", new string('a', 101), null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLastNameExceedsMaxLength()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", null, new string('b', 101));

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenFirstNameIsEmptyString()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", "", null);

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenLastNameIsEmptyString()
    {
        var request = new RegisterRequest("test@example.com", "Password1!", null, "");

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }
}
