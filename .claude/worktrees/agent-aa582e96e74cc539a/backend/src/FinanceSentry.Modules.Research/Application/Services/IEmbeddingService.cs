namespace FinanceSentry.Modules.Research.Application.Services;

public interface IEmbeddingService
{
    /// <summary>False when no provider is configured; indexing then stores chunks for lexical-only retrieval.</summary>
    bool IsEnabled { get; }

    string Provider { get; }

    string Model { get; }

    int Dimensions { get; }

    /// <summary>Embeds each text in order. Throws on provider failure; callers isolate failures per document.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
