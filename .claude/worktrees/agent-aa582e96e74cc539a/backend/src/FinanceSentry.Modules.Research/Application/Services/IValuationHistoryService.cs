namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Reconstructs a trailing-P/E history for a ticker from data we already source for free: EDGAR XBRL
/// diluted-EPS quarterly series rolled to TTM, divided into Yahoo daily closes (feature 030, R3). No
/// free source carries historical forward P/E or EV/EBITDA — those stay <c>historyUnavailable</c>.
/// </summary>
public interface IValuationHistoryService
{
    Task<TrailingPeHistory> GetTrailingPeHistoryAsync(string ticker, CancellationToken ct = default);
}

/// <summary>
/// The trailing-P/E history result. <see cref="FiveYearAvg"/> null = insufficient data (short of four
/// quarters, or no closes to price against) — never zero. <see cref="WindowYears"/> states the actual
/// window used so a recent IPO reports its real span, not a fabricated five years.
/// </summary>
public sealed record TrailingPeHistory(decimal? FiveYearAvg, int? WindowYears);
