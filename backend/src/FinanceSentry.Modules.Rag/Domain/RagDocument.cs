namespace FinanceSentry.Modules.Rag.Domain;

public sealed class RagDocument
{
    public Guid Id { get; private set; }
    public DocType DocType { get; private set; }

    /// <summary>FK into the source table (e.g. news_articles.id, theses.id).</summary>
    public Guid? SourceId { get; private set; }

    public string? Ticker { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Url { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>
    /// The date the information was considered current. Used for soft recency filtering.
    /// Defaults to PublishedAt but can be overridden (e.g. filing's period-end date).
    /// </summary>
    public DateTimeOffset AsOfDate { get; private set; }

    public DateTimeOffset IngestedAt { get; private set; }

    private RagDocument() { }

    public static RagDocument Create(
        DocType docType,
        string title,
        DateTimeOffset publishedAt,
        Guid? sourceId = null,
        string? ticker = null,
        string? url = null,
        DateTimeOffset? asOfDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new RagDocument
        {
            Id = Guid.NewGuid(),
            DocType = docType,
            SourceId = sourceId,
            Ticker = ticker,
            Title = title,
            Url = url,
            PublishedAt = publishedAt,
            AsOfDate = asOfDate ?? publishedAt,
            IngestedAt = DateTimeOffset.UtcNow,
        };
    }
}
