using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Categories.Create;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new CreateCategoryRequest("Groceries", TransactionType.Expense);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new CreateCategoryRequest("", TransactionType.Income);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var request = new CreateCategoryRequest(new string('a', 101), TransactionType.Expense);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionTypeIsInvalid()
    {
        var request = new CreateCategoryRequest("Test", (TransactionType)99);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }
}
