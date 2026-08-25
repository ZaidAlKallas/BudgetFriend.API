using FluentValidation;

namespace BudgetFriend.API.Features.Authentication.Refresh;

public sealed record RefreshRequest(string RefreshToken);

public sealed class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
