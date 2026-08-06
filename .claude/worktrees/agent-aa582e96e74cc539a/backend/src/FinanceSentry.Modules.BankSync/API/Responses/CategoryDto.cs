namespace FinanceSentry.Modules.BankSync.API.Responses;

/// <summary>A canonical spending category surfaced to the frontend for label resolution.</summary>
public record CategoryDto(string Key, string Label, int SortOrder);
