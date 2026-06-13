using BudgetFriend.API.Features.Authentication.Login;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class LoginValidatorTests
{
    private readonly LoginValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenBothFieldsAreValid()
    {
        var request = new LoginRequest("test@example.com", "password");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = new LoginRequest("", "password");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new LoginRequest("not-an-email", "password");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        var request = new LoginRequest("test@example.com", "");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
