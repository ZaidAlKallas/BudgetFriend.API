using BudgetFriend.API.Features.Accounts.Create;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new CreateAccountRequest("Checking Account", 1000m);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenInitialBalanceIsZero()
    {
        var request = new CreateAccountRequest("Checking Account", 0m);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new CreateAccountRequest("", 100m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var request = new CreateAccountRequest(new string('a', 101), 100m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInitialBalanceIsNegative()
    {
        var request = new CreateAccountRequest("Account", -1m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InitialBalance);
    }
}
