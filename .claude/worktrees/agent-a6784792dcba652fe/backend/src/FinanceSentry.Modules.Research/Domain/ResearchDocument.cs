namespace FinanceSentry.Modules.Research.Domain;

public class ResearchDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ResearchDocumentSourceType SourceType { get; set; }
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Null means a global research document visible to all authenticated users.</summary>
    public Guid? UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? CanonicalUrl { get; set; }
    public string? SourceName { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ContentHash { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Tickers { get; set; } = [];
    public List<Guid> ThesisIds { get; set; } = [];
    public ResearchIndexStatus IndexStatus { get; set; } = ResearchIndexStatus.Pending;
    public string? IndexFailureReason { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
}
