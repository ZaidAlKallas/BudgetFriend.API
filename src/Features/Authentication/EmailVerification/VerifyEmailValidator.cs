using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(2048).WithMessage("Token must be 2048 characters or fewer.");
    }
}