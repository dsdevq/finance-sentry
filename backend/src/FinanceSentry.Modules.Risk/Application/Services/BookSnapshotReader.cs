namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Domain;

/// <summary>
/// Adapts <see cref="IBookFiguresReader"/> — the canonical book-figures service — into the
/// Risk-module-local <see cref="BookSnapshot"/> shape. Risk's internal domain (evaluation service,
/// jobs, tests) consumes BookSnapshot; this adapter keeps that surface unchanged while ensuring all
/// cash / invested-value figures come from the single source of truth.
/// </summary>
public sealed class BookSnapshotReader(IBookFiguresReader bookFiguresReader) : IBookSnapshotReader
{
    public async Task<BookSnapshot> ReadAsync(Guid userId, CancellationToken ct = default)
    {
        var figures = await bookFiguresReader.ReadAsync(userId, ct);

        // Map BookFiguresPosition → BookPosition, preserving the Sleeve concept the risk
        // evaluation service uses for sleeve-weight rules (brokerage/crypto).
        var positions = figures.Positions
            .Select(p => new BookPosition(
                p.Symbol,
                ToRiskSleeve(p.AssetClass),
                p.Quantity,
                p.UsdValue,
                p.WeightPct))
            .ToList();

        return new BookSnapshot(
            TotalUsd: figures.TotalUsd,
            CashUsd: figures.CashUsd,
            BankingCashUsd: figures.BankingCashUsd,
            BrokerageCashUsd: figures.BrokerageCashUsd,
            Positions: positions,
            IsStale: figures.IsStale,
            StaleSources: figures.StaleSources);
    }

    private static string ToRiskSleeve(string assetClass) => assetClass switch
    {
        Core.Utils.AssetClassNormalizer.Crypto => RiskSleeve.Crypto,
        _ => RiskSleeve.Brokerage,
    };
}
