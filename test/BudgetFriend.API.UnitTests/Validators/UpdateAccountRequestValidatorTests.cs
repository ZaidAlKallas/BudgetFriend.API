using BudgetFriend.API.Features.Accounts.Update;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class UpdateAccountRequestValidatorTests
{
    private readonly UpdateAccountRequestValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new UpdateAccountRequest("Updated Account", 500m);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenInitialBalanceIsZero()
    {
        var request = new UpdateAccountRequest("Updated Account", 0m);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new UpdateAccountRequest("", 100m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var request = new UpdateAccountRequest(new string('a', 101), 100m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInitialBalanceIsNegative()
    {
        var request = new UpdateAccountRequest("Account", -1m);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InitialBalance);
    }
}
