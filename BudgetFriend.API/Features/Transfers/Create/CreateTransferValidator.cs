using FluentValidation;

namespace BudgetFriend.API.Features.Transfers.Create;

public sealed class CreateTransferValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty();

        RuleFor(x => x.ToAccountId)
            .NotEmpty();

        RuleFor(x => x.FromAccountId)
            .NotEqual(x => x.ToAccountId)
            .WithMessage("Source and destination accounts must be different.");

        RuleFor(x => x.FromAmount)
            .GreaterThan(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.ToAmount)
            .GreaterThan(0)
            .PrecisionScale(18, 2, false);
    }
}
