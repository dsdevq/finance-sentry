namespace FinanceSentry.Modules.Alerts.Domain;

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = AlertType.LowBalance;
    public string Severity { get; set; } = AlertSeverity.Warning;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceLabel { get; set; }
    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public bool IsDismissed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
