namespace FinanceSentry.Modules.BankSync.API.Extensions;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Pagination helpers for IQueryable endpoints.
/// </summary>
public static class PaginationExtensions
{
    public const int MaxLimit = 100;
    public const int DefaultLimit = 50;

    /// <summary>
    /// Validates and applies offset/limit pagination to a query.
    /// Clamps limit to 1-100. Throws ArgumentException on invalid inputs.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        int offset,
        int limit)
    {
        if (offset < 0)
            throw new ArgumentException("Offset must be >= 0.", nameof(offset));
        if (limit < 1 || limit > MaxLimit)
            throw new ArgumentException($"Limit must be between 1 and {MaxLimit}.", nameof(limit));

        return query.Skip(offset).Take(limit);
    }

    /// <summary>
    /// Wraps a result set in standard pagination metadata.
    /// </summary>
    public static PaginatedResponse<T> CreatePaginatedResponse<T>(
        IEnumerable<T> items,
        int totalCount,
        int offset,
        int limit)
    {
        var itemList = items.ToList();
        return new PaginatedResponse<T>
        {
            Items = itemList,
            TotalCount = totalCount,
            Offset = offset,
            Limit = limit,
            HasMore = (offset + itemList.Count) < totalCount
        };
    }

    /// <summary>
    /// Returns 400 BadRequest if pagination params are invalid.
    /// Returns null if valid (caller should proceed normally).
    /// </summary>
    public static IActionResult? ValidatePagination(
        ControllerBase controller,
        int offset,
        int limit)
    {
        if (offset < 0)
            return controller.BadRequest(new { error = "offset must be >= 0" });
        if (limit < 1 || limit > MaxLimit)
            return controller.BadRequest(new { error = $"limit must be between 1 and {MaxLimit}" });
        return null;
    }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public bool HasMore { get; set; }
}
