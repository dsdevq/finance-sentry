namespace FinanceSentry.Core.Api;

public record PagedRequest(int Offset = 0, int Limit = 50);
