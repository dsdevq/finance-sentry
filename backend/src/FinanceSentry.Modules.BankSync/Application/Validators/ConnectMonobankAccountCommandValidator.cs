using FinanceSentry.Modules.BankSync.Application.Commands;
using FluentValidation;

namespace FinanceSentry.Modules.BankSync.Application.Validators;

public sealed class ConnectMonobankAccountCommandValidator : AbstractValidator<ConnectMonobankAccountCommand>
{
    private const int MaxTokenLength = 64;

    public ConnectMonobankAccountCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(MaxTokenLength).WithMessage($"Token must be at most {MaxTokenLength} characters.");
    }
}
