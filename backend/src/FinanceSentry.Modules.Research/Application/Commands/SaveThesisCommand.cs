namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Application.Validation;
using FinanceSentry.Modules.Research.Domain.Exceptions;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record SaveThesisCommand(
    Guid UserId,
    Guid? Id,
    string Ticker,
    string ThesisText,
    IReadOnlyList<ThesisDataPoint> KeyDataPoints,
    IReadOnlyList<ThesisCatalyst> Catalysts,
    IReadOnlyList<ThesisInvalidationTrigger> InvalidationTriggers,
    decimal? EntryPrice = null,
    string? DecisionNote = null) : ICommand<ThesisDto>;

public class SaveThesisCommandHandler(IThesisRepository repo, IThesisEventRecorder eventRecorder)
    : ICommandHandler<SaveThesisCommand, ThesisDto>
{
    public async Task<ThesisDto> Handle(SaveThesisCommand cmd, CancellationToken ct)
    {
        ThesisTriggerVocabulary.Validate(cmd.InvalidationTriggers);

        InvestmentThesis thesis;
        var isNewThesis = cmd.Id is null;

        if (cmd.Id is { } id)
        {
            var existing = await repo.FindAsync(cmd.UserId, id, ct)
                ?? throw new ThesisNotFoundException();
            thesis = existing;
        }
        else
        {
            thesis = new InvestmentThesis { UserId = cmd.UserId };
        }

        thesis.Ticker = cmd.Ticker.Trim().ToUpperInvariant();
        thesis.ThesisText = cmd.ThesisText.Trim();
        thesis.EntryPrice = cmd.EntryPrice;
        thesis.KeyDataPoints = cmd.KeyDataPoints.ToList();
        thesis.Catalysts = cmd.Catalysts.ToList();
        thesis.InvalidationTriggers = cmd.InvalidationTriggers.ToList();

        await repo.UpsertAsync(thesis, ct);

        if (isNewThesis)
        {
            // FR-001/FR-002: one Created event per thesis, never on update. A quote failure here
            // must never surface to the caller (FR-003) — the recorder itself never throws.
            await eventRecorder.RecordAsync(
                thesis.UserId,
                ThesisSubjectType.Thesis,
                thesis.Id,
                thesis.Ticker,
                ThesisEventType.Created,
                cmd.DecisionNote,
                ct);
        }

        return new ThesisDto(
            thesis.Id, thesis.Ticker, thesis.ThesisText,
            thesis.KeyDataPoints, thesis.Catalysts, thesis.InvalidationTriggers,
            thesis.CreatedAt, thesis.UpdatedAt, thesis.BrokenAt, thesis.BrokenReason, thesis.EntryPrice);
    }
}
