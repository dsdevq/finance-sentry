namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Core-facing read of broken theses (017) — lets other modules (Risk) check whether a ticker's
/// thesis is broken without depending on the Research module's internal repository.
/// </summary>
public interface IBrokenThesisReader
{
    Task<IReadOnlyList<BrokenThesisSummary>> ListBrokenAsync(Guid userId, CancellationToken ct = default);
}

public sealed record BrokenThesisSummary(Guid ThesisId, string Ticker, DateTimeOffset BrokenAt);
