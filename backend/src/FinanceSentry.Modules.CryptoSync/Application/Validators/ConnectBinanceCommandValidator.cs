using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FluentValidation;

namespace FinanceSentry.Modules.CryptoSync.Application.Validators;

public sealed class ConnectBinanceCommandValidator : AbstractValidator<ConnectBinanceCommand>
{
    public ConnectBinanceCommandValidator()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("apiKey is required.");

        RuleFor(x => x.ApiSecret)
            .NotEmpty().WithMessage("apiSecret is required.");
    }
}
