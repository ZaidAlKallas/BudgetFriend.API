using BudgetFriend.API.Database.Enums;
using FluentValidation;

namespace BudgetFriend.API.Features.Transactions.Create;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty();

        When(x => x.TransactionType == TransactionType.Income || x.TransactionType == TransactionType.Expense, () =>
        {
            RuleFor(x => x.CategoryId)
                .NotNull()
                .NotEqual(Guid.Empty);
        });

        RuleFor(x => x.Amount)
            .NotEqual(0)
            .PrecisionScale(18, 2, false);
    }
}
