using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must be 128 characters or fewer.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("\\d").WithMessage("Password must contain at least one digit.")
            .Matches("[\\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must be 100 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must be 100 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}
