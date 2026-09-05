using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");
    }
}