using FluentValidation;

namespace BudgetFriend.API.Features.Accounts.Create;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountRequest> {
    public CreateAccountValidator() {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0);
    }
}
