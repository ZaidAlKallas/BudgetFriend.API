using FluentValidation;

namespace BudgetFriend.API.Features.Transactions.Create;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .NotEqual(0)
            .PrecisionScale(18, 2, false);
    }
}
