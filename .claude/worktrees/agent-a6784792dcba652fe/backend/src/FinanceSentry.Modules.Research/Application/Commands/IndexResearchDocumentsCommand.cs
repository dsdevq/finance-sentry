namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Application.Services;

/// <summary>Manually triggers one research indexing run (the recurring job runs the same path).</summary>
public record IndexResearchDocumentsCommand : ICommand<ResearchIndexingResult>;

public class IndexResearchDocumentsCommandHandler(IResearchIndexer indexer)
    : ICommandHandler<IndexResearchDocumentsCommand, ResearchIndexingResult>
{
    public Task<ResearchIndexingResult> Handle(IndexResearchDocumentsCommand command, CancellationToken ct)
        => indexer.IndexPendingAsync(ct);
}
