using BudgetFriend.API.Database.Enums;
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
            .IsInEnum();
    }
}
