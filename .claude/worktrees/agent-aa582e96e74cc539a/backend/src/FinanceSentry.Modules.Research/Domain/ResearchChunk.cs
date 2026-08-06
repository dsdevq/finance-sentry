namespace FinanceSentry.Modules.Research.Domain;

public class ResearchChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int Ordinal { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int TokenEstimate { get; set; }
    public int? StartOffset { get; set; }
    public int? EndOffset { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
