namespace FinanceSentry.Modules.Research.API.Responses;

using FinanceSentry.Modules.Research.Domain;

public record ThesisDto(
    Guid Id,
    string Ticker,
    string ThesisText,
    IReadOnlyList<ThesisDataPoint> KeyDataPoints,
    IReadOnlyList<ThesisCatalyst> Catalysts,
    IReadOnlyList<ThesisInvalidationTrigger> InvalidationTriggers,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? BrokenAt,
    string? BrokenReason);
