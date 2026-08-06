namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Modules.Research.Application.Services;
using Microsoft.Extensions.Logging;

public class ResearchIndexingJob(IResearchIndexer indexer, ILogger<ResearchIndexingJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var result = await indexer.IndexPendingAsync(ct);
        if (result.Failed > 0)
        {
            logger.LogWarning(
                "Research indexing completed with {Failed} failed documents ({Indexed} indexed, {Skipped} skipped)",
                result.Failed, result.Indexed, result.Skipped);
        }
    }
}
