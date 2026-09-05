namespace FinanceSentry.Modules.Research.Domain.Ports;

/// <summary>
/// Cross-module port (feature 421). Provides the Asset Dossier with IBKR tax-lot detail for a
/// specific symbol. The concrete adapter lives in FinanceSentry.Integration so Modules.Research
/// never depends on Modules.BrokerageSync directly.
/// </summary>
public interface IHoldingTaxLotsReader
{
    /// <summary>
    /// Returns tax lot detail for the given symbol, or null when the user holds no position
    /// in that symbol via IBKR. Crypto symbols return null (no lot-level history available).
    /// </summary>
    Task<IReadOnlyList<DossierTaxLotEntry>?> GetForSymbolAsync(
        Guid userId, string symbol, CancellationToken ct = default);
}

/// <summary>One IBKR tax lot for a dossier position.</summary>
public sealed record DossierTaxLotEntry(
    decimal Quantity,
    decimal CurrentValueUsd,
    decimal? AverageCostUsd,
    decimal? CostBasisUsd,
    decimal? UnrealizedPnlUsd,
    decimal? UnrealizedPnlPercent,
    DateTime? AcquiredAt,
    bool IsLongTerm);
