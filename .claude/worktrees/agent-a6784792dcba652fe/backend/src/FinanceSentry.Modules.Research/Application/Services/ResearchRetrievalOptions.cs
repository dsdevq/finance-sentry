namespace FinanceSentry.Modules.Research.Application.Services;

public sealed class ResearchRetrievalOptions
{
    public const string SectionName = "ResearchRetrieval";

    /// <summary>Target chunk size in characters. Chunk boundaries snap back to whitespace.</summary>
    public int ChunkSizeChars { get; set; } = 1600;

    /// <summary>Characters of overlap carried between consecutive chunks.</summary>
    public int ChunkOverlapChars { get; set; } = 200;

    /// <summary>Hard cap on chunks per document; overlong documents are truncated at this bound.</summary>
    public int MaxChunksPerDocument { get; set; } = 64;

    /// <summary>Documents processed per repository fetch inside one indexing run.</summary>
    public int IndexingBatchSize { get; set; } = 25;

    /// <summary>Upper bound on documents one indexing run will process before yielding.</summary>
    public int MaxDocumentsPerRun { get; set; } = 500;

    /// <summary>Upper bound on candidate chunks loaded for one search before in-app ranking.</summary>
    public int MaxSearchCandidates { get; set; } = 2000;

    public int DefaultSearchLimit { get; set; } = 10;

    public int MaxSearchLimit { get; set; } = 50;

    /// <summary>Default chunk budget for a context packet.</summary>
    public int ContextMaxChunks { get; set; } = 12;

    /// <summary>Hard cap a caller-supplied maxChunks is clamped to.</summary>
    public int ContextMaxChunksCap { get; set; } = 30;

    /// <summary>Weight of the semantic score in the combined score when an embedding is available.</summary>
    public double SemanticWeight { get; set; } = 0.65;

    /// <summary>Bump when chunking or embedding rules change to force reindexing.</summary>
    public int EmbeddingVersion { get; set; } = 1;

    public EmbeddingProviderOptions Embedding { get; set; } = new();

    public sealed class EmbeddingProviderOptions
    {
        /// <summary>When false (default), indexing stores chunks only and retrieval ranks lexically.</summary>
        public bool Enabled { get; set; }

        public string Provider { get; set; } = "openai";

        /// <summary>OpenAI-compatible API root, e.g. https://api.openai.com/v1.</summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        public string Model { get; set; } = "text-embedding-3-small";

        public int Dimensions { get; set; } = 1536;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Chunk texts sent per embedding API call.</summary>
        public int BatchSize { get; set; } = 32;

        public int TimeoutSeconds { get; set; } = 30;
    }
}
