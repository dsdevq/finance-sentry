using FinanceSentry.Modules.Auth.Application.Commands;
using FluentValidation;

namespace FinanceSentry.Modules.Auth.Application.Validators;

public sealed class VerifyGoogleCredentialCommandValidator : AbstractValidator<VerifyGoogleCredentialCommand>
{
    public VerifyGoogleCredentialCommandValidator()
    {
        RuleFor(x => x.Credential)
            .NotEmpty().WithMessage("Credential is required.");
    }
}
