using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Categories.Update;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class UpdateCategoryRequestValidatorTests
{
    private readonly UpdateCategoryRequestValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new UpdateCategoryRequest("Updated Category", TransactionType.Income);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new UpdateCategoryRequest("", TransactionType.Expense);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var request = new UpdateCategoryRequest(new string('a', 101), TransactionType.Expense);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionTypeIsInvalid()
    {
        var request = new UpdateCategoryRequest("Test", (TransactionType)99);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }
}
