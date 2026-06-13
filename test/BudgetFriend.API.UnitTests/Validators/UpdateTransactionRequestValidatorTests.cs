using BudgetFriend.API.Features.Transactions.Update;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetFriend.API.UnitTests.Validators;

public sealed class UpdateTransactionRequestValidatorTests
{
    private readonly UpdateTransactionRequestValidator _sut = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var request = new UpdateTransactionRequest(100m, "Updated note", DateTime.UtcNow);

        var result = _sut.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountIsZero()
    {
        var request = new UpdateTransactionRequest(0m, null, DateTime.UtcNow);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTransactionDateIsEmpty()
    {
        var request = new UpdateTransactionRequest(50m, null, default);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionDate);
    }
}
