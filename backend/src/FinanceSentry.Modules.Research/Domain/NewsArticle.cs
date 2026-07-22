namespace FinanceSentry.Modules.Research.Domain;

public class NewsArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<string> Tickers { get; set; } = [];
    public List<string> Categories { get; set; } = [];

    /// <summary>Theses this article matched — source-registered and/or keyword match (feature 030, FR-008).</summary>
    public List<Guid> ThesisIds { get; set; } = [];
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ContentHash { get; set; } = string.Empty;
}
