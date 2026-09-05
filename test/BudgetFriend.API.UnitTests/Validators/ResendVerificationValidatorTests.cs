using BudgetFriend.API.Features.Authentication.EmailVerification;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class ResendVerificationValidatorTests
{
    private readonly ResendVerificationValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenEmailIsValid()
    {
        var request = new ResendVerificationRequest("test@example.com");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        var request = new ResendVerificationRequest("");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new ResendVerificationRequest("not-an-email");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailExceedsMaxLength()
    {
        var request = new ResendVerificationRequest(new string('a', 257) + "@example.com");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}