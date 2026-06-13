using BudgetFriend.API.Features.Transactions.Create;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, "Test", DateTime.UtcNow);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenNoteIsNull()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 50m, null, null);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAccountIdIsEmpty()
    {
        var request = new CreateTransactionRequest(Guid.Empty, Guid.NewGuid(), 100m, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCategoryIdIsEmpty()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.Empty, 100m, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountIsZero()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 0m, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountExceedsPrecision()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 999999999999999999m, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }
}
