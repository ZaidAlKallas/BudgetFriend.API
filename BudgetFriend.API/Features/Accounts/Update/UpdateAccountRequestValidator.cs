using FluentValidation;

namespace BudgetFriend.API.Features.Accounts.Update;

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest> {
    public UpdateAccountRequestValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
    }
}
