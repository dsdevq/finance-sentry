namespace FinanceSentry.Integration;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.Research.Domain.Ports;

/// <summary>
/// Feature 421 — implements <see cref="IHoldingTaxLotsReader"/> by delegating to the BrokerageSync
/// module's <see cref="GetTaxLotsQuery"/>. Lives in Integration so Modules.Research never references
/// Modules.BrokerageSync directly.
/// </summary>
public sealed class HoldingTaxLotsAdapter(
    IQueryHandler<GetTaxLotsQuery, TaxLotsResponse> getTaxLots) : IHoldingTaxLotsReader
{
    public async Task<IReadOnlyList<DossierTaxLotEntry>?> GetForSymbolAsync(
        Guid userId, string symbol, CancellationToken ct = default)
    {
        var response = await getTaxLots.Handle(new GetTaxLotsQuery(userId), ct);

        var matching = response.Items
            .Where(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Return null when the user has no position in this symbol.
        if (matching.Count == 0)
        {
            return null;
        }

        return matching
            .Select(i => new DossierTaxLotEntry(
                Quantity: i.Quantity,
                CurrentValueUsd: i.CurrentValueUsd,
                AverageCostUsd: i.AverageCostUsd,
                CostBasisUsd: i.CostBasisUsd,
                UnrealizedPnlUsd: i.UnrealizedPnlUsd,
                UnrealizedPnlPercent: i.UnrealizedPnlPercent,
                AcquiredAt: i.AcquiredAt,
                IsLongTerm: i.IsLongTerm))
            .ToList();
    }
}
