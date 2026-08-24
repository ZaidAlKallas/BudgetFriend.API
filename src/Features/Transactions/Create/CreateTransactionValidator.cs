using FluentValidation;

namespace BudgetFriend.API.Features.Transactions.Create;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .Must(t => t is TransactionType.Income or TransactionType.Expense)
            .WithMessage("Only Income and Expense transaction types are allowed.");

        RuleFor(x => x.Amount)
            .NotEqual(0)
            .PrecisionScale(18, 2, false);
    }
}
