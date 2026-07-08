namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain;
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
    IReadOnlyList<ThesisInvalidationTrigger> InvalidationTriggers) : ICommand<ThesisDto>;

public class SaveThesisCommandHandler(IThesisRepository repo)
    : ICommandHandler<SaveThesisCommand, ThesisDto>
{
    public async Task<ThesisDto> Handle(SaveThesisCommand cmd, CancellationToken ct)
    {
        ThesisTriggerVocabulary.Validate(cmd.InvalidationTriggers);

        InvestmentThesis thesis;

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
        thesis.KeyDataPoints = cmd.KeyDataPoints.ToList();
        thesis.Catalysts = cmd.Catalysts.ToList();
        thesis.InvalidationTriggers = cmd.InvalidationTriggers.ToList();

        await repo.UpsertAsync(thesis, ct);

        return new ThesisDto(
            thesis.Id, thesis.Ticker, thesis.ThesisText,
            thesis.KeyDataPoints, thesis.Catalysts, thesis.InvalidationTriggers,
            thesis.CreatedAt, thesis.UpdatedAt, thesis.BrokenAt, thesis.BrokenReason);
    }
}
