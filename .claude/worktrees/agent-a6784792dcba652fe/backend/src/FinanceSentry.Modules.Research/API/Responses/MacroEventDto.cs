namespace FinanceSentry.Modules.Research.API.Responses;

public record MacroEventDto(
    Guid Id,
    DateOnly EventDate,
    TimeOnly? EventTime,
    string Event,
    string Region,
    string Importance,
    string Source);
