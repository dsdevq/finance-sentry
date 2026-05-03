namespace FinanceSentry.Modules.Alerts.API.Responses;

public record AlertsPageResponse(
    IReadOnlyList<AlertDto> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalPages);
