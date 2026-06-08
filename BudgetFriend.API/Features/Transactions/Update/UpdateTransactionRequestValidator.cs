using FluentValidation;

namespace BudgetFriend.API.Features.Transactions.Update;

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.TransactionDate)
            .NotEmpty();
    }
}
