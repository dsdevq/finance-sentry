namespace FinanceSentry.Modules.Risk.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Risk.Application.Queries;
using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Repositories;

public record SaveRiskRuleSetCommand(
    Guid UserId,
    decimal? MaxPositionWeightPct,
    decimal? MaxSleeveWeightPct,
    decimal? MinCashBufferPct,
    decimal? MaxLossPerThesisPct,
    decimal? MaxNewPositionPct,
    int? TurnoverBudgetPerQuarter) : ICommand<RiskRuleSetDto>;

public sealed class RiskRuleSetValidationException(string message) : Exception(message);

public sealed class SaveRiskRuleSetCommandHandler(IRiskRuleSetRepository repo)
    : ICommandHandler<SaveRiskRuleSetCommand, RiskRuleSetDto>
{
    public async Task<RiskRuleSetDto> Handle(SaveRiskRuleSetCommand command, CancellationToken ct)
    {
        Validate(command);

        var entity = new RiskRuleSet
        {
            UserId = command.UserId,
            MaxPositionWeightPct = command.MaxPositionWeightPct,
            MaxSleeveWeightPct = command.MaxSleeveWeightPct,
            MinCashBufferPct = command.MinCashBufferPct,
            MaxLossPerThesisPct = command.MaxLossPerThesisPct,
            MaxNewPositionPct = command.MaxNewPositionPct,
            TurnoverBudgetPerQuarter = command.TurnoverBudgetPerQuarter,
        };

        var saved = await repo.SaveNewVersionAsync(entity, ct);
        return RiskRuleSetDto.FromEntity(saved);
    }

    private static void Validate(SaveRiskRuleSetCommand command)
    {
        ValidateFractionalRange(command.MaxPositionWeightPct, nameof(command.MaxPositionWeightPct));
        ValidateFractionalRange(command.MaxSleeveWeightPct, nameof(command.MaxSleeveWeightPct));
        ValidateFractionalRange(command.MinCashBufferPct, nameof(command.MinCashBufferPct));
        ValidateFractionalRange(command.MaxLossPerThesisPct, nameof(command.MaxLossPerThesisPct));
        ValidateFractionalRange(command.MaxNewPositionPct, nameof(command.MaxNewPositionPct));

        if (command.TurnoverBudgetPerQuarter is < 0)
        {
            throw new RiskRuleSetValidationException(
                $"{nameof(command.TurnoverBudgetPerQuarter)} must be non-negative if set.");
        }
    }

    private static void ValidateFractionalRange(decimal? value, string fieldName)
    {
        if (value is null)
        {
            return;
        }

        if (value is <= 0 or > 1)
        {
            throw new RiskRuleSetValidationException($"{fieldName} must be in (0, 1] if set.");
        }
    }
}
