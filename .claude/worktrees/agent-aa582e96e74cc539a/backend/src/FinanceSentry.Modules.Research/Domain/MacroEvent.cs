namespace FinanceSentry.Modules.Research.Domain;

public class MacroEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly EventDate { get; set; }
    public TimeOnly? EventTime { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Region { get; set; } = "US";
    public string Importance { get; set; } = MacroImportance.Medium;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class MacroImportance
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
}
