namespace FinanceSentry.Modules.Research.Domain;

public class ResearchEmbedding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChunkId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Dimensions { get; set; }

    /// <summary>Increments when chunking or embedding rules change; stale versions are ignored at query time.</summary>
    public int EmbeddingVersion { get; set; }

    public float[] Vector { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
