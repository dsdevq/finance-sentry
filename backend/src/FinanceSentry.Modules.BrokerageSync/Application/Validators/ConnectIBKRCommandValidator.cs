using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FluentValidation;

namespace FinanceSentry.Modules.BrokerageSync.Application.Validators;

public sealed class ConnectIBKRCommandValidator : AbstractValidator<ConnectIBKRCommand>
{
    public ConnectIBKRCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
