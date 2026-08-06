namespace FinanceSentry.Modules.BankSync.API.Extensions;

using FinanceSentry.Core.Api;
using Microsoft.AspNetCore.Mvc;

public static class PaginationExtensions
{
    public const int MaxLimit = 100;
    public const int DefaultLimit = 50;

    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int offset, int limit)
    {
        if (offset < 0)
            throw new ArgumentException("Offset must be >= 0.", nameof(offset));
        if (limit < 1 || limit > MaxLimit)
            throw new ArgumentException($"Limit must be between 1 and {MaxLimit}.", nameof(limit));

        return query.Skip(offset).Take(limit);
    }

    public static PagedResponse<T> CreatePaginatedResponse<T>(
        IEnumerable<T> items,
        int totalCount,
        int offset,
        int limit)
    {
        var itemList = items.ToList();
        return new PagedResponse<T>(
            itemList,
            totalCount,
            offset,
            limit,
            (offset + itemList.Count) < totalCount);
    }

    public static IActionResult? ValidatePagination(ControllerBase controller, int offset, int limit)
    {
        if (offset < 0)
            return controller.BadRequest(new ApiErrorBody("offset must be >= 0", "INVALID_PAGINATION"));
        if (limit < 1 || limit > MaxLimit)
            return controller.BadRequest(
                new ApiErrorBody($"limit must be between 1 and {MaxLimit}", "INVALID_PAGINATION"));
        return null;
    }
}
