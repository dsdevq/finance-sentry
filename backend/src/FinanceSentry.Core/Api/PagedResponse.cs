namespace FinanceSentry.Core.Api;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Offset,
    int Limit,
    bool HasMore);
