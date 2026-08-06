namespace FinanceSentry.Modules.Risk.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;

/// <summary>
/// FR-007: overrides of a Refused verdict MUST be recorded as signals — never silent. Writes both
/// the shared radar_signals log (018) and an Alert so the override is visible in the track record.
/// </summary>
public record RecordRiskOverrideCommand(
    Guid UserId, string RuleKey, string Subject, decimal ObservedValue, decimal LimitValue) : ICommand<Unit>;

public sealed class RecordRiskOverrideCommandHandler(
    IRadarSignalWriter signalWriter,
    IAlertGeneratorService alertGenerator)
    : ICommandHandler<RecordRiskOverrideCommand, Unit>
{
    public async Task<Unit> Handle(RecordRiskOverrideCommand command, CancellationToken ct)
    {
        var dedupKey = $"risk_rules:override:{command.UserId}:{command.RuleKey}:{command.Subject}:{DateTimeOffset.UtcNow:yyyy-MM-dd}";

        await signalWriter.AppendSignalAsync(new RadarSignalRequest(
            Scanner: "risk_rules",
            SignalType: "risk_override",
            Severity: SignalSeverity.Notable,
            SubjectType: "Ticker",
            Subject: command.Subject,
            UserId: command.UserId,
            DedupKey: dedupKey,
            Payload: new
            {
                command.RuleKey,
                command.ObservedValue,
                command.LimitValue,
            }), ct);

        await alertGenerator.GeneratePolicyViolationAlertAsync(
            command.UserId,
            command.RuleKey,
            command.Subject,
            command.ObservedValue,
            command.LimitValue,
            isOverride: true,
            ct);

        return Unit.Value;
    }
}
