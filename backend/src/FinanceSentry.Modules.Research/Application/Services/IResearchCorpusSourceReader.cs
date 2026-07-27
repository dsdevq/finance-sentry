namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IResearchCorpusSourceReader
{
    /// <summary>
    /// Projects current stored source entities (news articles, theses, decision notes) into
    /// unsaved <see cref="ResearchDocument"/> instances with computed content hashes. The indexer
    /// diffs these against persisted documents to decide what is new or changed.
    /// </summary>
    Task<IReadOnlyList<ResearchDocument>> LoadSourceDocumentsAsync(CancellationToken ct = default);
}
