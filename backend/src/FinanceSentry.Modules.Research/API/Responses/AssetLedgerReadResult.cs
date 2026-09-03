namespace FinanceSentry.Modules.Research.API.Responses;

/// <summary>
/// "Ledger's read" for one ticker (feature 421, US3).
/// <para><c>Narrative</c> is null when nothing has been generated yet — the UI shows the generate CTA.</para>
/// <para><c>IsStale</c> is true when the cached copy is older than a day or the dossier facts it was
/// generated from have since moved; the UI still renders it, flagged as out of date.</para>
/// <para><c>Cached</c> distinguishes a copy served from storage (instant) from one just generated.</para>
/// </summary>
public sealed record AssetLedgerReadResult(
    string Symbol,
    string? Narrative,
    DateTimeOffset? GeneratedAt,
    bool IsStale,
    bool Cached);
