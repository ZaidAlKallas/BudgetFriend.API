using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(2048).WithMessage("Token must be 2048 characters or fewer.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must be 128 characters or fewer.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("\\d").WithMessage("Password must contain at least one digit.")
            .Matches("[\\W_]").WithMessage("Password must contain at least one special character.");
    }
}