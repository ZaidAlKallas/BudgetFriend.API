using BudgetFriend.API.Database.Enums;
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
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, TransactionType.Expense, "Test", DateTime.UtcNow);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenNoteIsNull()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 50m, TransactionType.Income, null, null);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAccountIdIsEmpty()
    {
        var request = new CreateTransactionRequest(Guid.Empty, Guid.NewGuid(), 100m, TransactionType.Expense, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCategoryIdIsEmpty_ForIncomeTransaction()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.Empty, 100m, TransactionType.Income, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCategoryIdIsEmpty_ForExpenseTransaction()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.Empty, 100m, TransactionType.Expense, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionTypeIsTransferIn()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, TransactionType.TransferIn, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionTypeIsTransferOut()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, TransactionType.TransferOut, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionTypeIsNotInEnum()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, (TransactionType)999, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountIsZero()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 0m, TransactionType.Expense, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountExceedsPrecision()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 999999999999999999m, TransactionType.Expense, null, null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }
}
