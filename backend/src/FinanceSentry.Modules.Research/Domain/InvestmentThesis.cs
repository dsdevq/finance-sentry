namespace FinanceSentry.Modules.Research.Domain;

public class InvestmentThesis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string ThesisText { get; set; } = string.Empty;
    public List<ThesisDataPoint> KeyDataPoints { get; set; } = [];
    public List<ThesisCatalyst> Catalysts { get; set; } = [];
    public List<ThesisInvalidationTrigger> InvalidationTriggers { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? BrokenAt { get; set; }
    public string? BrokenReason { get; set; }
}

public record ThesisDataPoint(string Metric, string Direction, decimal Threshold, string Source);

public record ThesisCatalyst(DateOnly Date, string Event);

public record ThesisInvalidationTrigger(string Metric, string Direction, decimal Threshold);
