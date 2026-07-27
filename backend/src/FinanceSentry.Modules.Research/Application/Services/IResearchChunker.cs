namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IResearchChunker
{
    /// <summary>
    /// Splits a document's text into deterministic chunks with stable ordinals, content hashes,
    /// and source offsets. Returns an empty list when the document has no usable text.
    /// </summary>
    IReadOnlyList<ResearchChunk> Chunk(ResearchDocument document);
}
