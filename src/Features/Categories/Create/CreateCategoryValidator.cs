using FluentValidation;

namespace BudgetFriend.API.Features.Categories.Create;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .Must(t => t is TransactionType.Income or TransactionType.Expense)
            .WithMessage("Only Income and Expense transaction types are allowed.");
    }
}
