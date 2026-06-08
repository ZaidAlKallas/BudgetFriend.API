using BudgetFriend.API.Database.Enums;
using FluentValidation;

namespace BudgetFriend.API.Features.Categories.Update;

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TransactionType)
            .IsInEnum();
    }
}
