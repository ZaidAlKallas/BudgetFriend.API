using BudgetFriend.API.Features.Authentication.EmailVerification;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class VerifyEmailValidatorTests
{
    private readonly VerifyEmailValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenTokenIsProvided()
    {
        var request = new VerifyEmailRequest("valid-token");

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTokenIsEmpty()
    {
        var request = new VerifyEmailRequest("");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTokenIsNull()
    {
        var request = new VerifyEmailRequest(null!);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTokenExceedsMaxLength()
    {
        var request = new VerifyEmailRequest(new string('a', 2049));

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}