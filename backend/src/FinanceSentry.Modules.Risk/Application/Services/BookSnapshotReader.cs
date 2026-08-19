namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Builds a <see cref="BookSnapshot"/> from the canonical <see cref="IBookFiguresService"/>,
/// mapping normalized asset classes to <see cref="RiskSleeve"/> labels and computing
/// per-position weights from the book total.
/// </summary>
public sealed class BookSnapshotReader(
    IBookFiguresService bookFigures,
    ILogger<BookSnapshotReader> logger) : IBookSnapshotReader
{
    public async Task<BookSnapshot> ReadAsync(Guid userId, CancellationToken ct = default)
    {
        BookFigures book;
        try
        {
            book = await bookFigures.ReadAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Book figures unavailable for risk check of user {UserId}; returning empty snapshot.", userId);
            return new BookSnapshot(0m, 0m, [], true, ["all"], 0m);
        }

        if (book.IsStale)
            logger.LogWarning("Book figures stale for user {UserId}: {Sources}.", userId, string.Join(", ", book.StaleSources));

        var positions = book.Positions
            .Select(p => new BookPosition(
                p.Symbol,
                ToRiskSleeve(p.AssetClass),
                p.Quantity,
                p.UsdValue,
                book.TotalValueUsd > 0 ? p.UsdValue / book.TotalValueUsd : 0m))
            .ToList();

        return new BookSnapshot(book.TotalValueUsd, book.CashUsd, positions, book.IsStale, book.StaleSources, book.InvestedValueUsd);
    }

    private static string ToRiskSleeve(string assetClass) => assetClass == AssetClassNormalizer.Crypto
        ? RiskSleeve.Crypto
        : RiskSleeve.Brokerage;
}
