namespace FinanceSentry.Modules.Rag.Domain;

/// <summary>
/// Converts a text passage into a dense embedding vector (1024 dimensions, L2-normalised).
/// </summary>
public interface IEmbeddingClient
{
    /// <returns>A unit-length float[1024] vector.</returns>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
