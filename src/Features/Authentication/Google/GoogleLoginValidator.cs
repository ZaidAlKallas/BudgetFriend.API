using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.Google;

public sealed class GoogleLoginValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("IdToken is required.")
            .MaximumLength(8192).WithMessage("IdToken must be 8192 characters or fewer.");
    }
}