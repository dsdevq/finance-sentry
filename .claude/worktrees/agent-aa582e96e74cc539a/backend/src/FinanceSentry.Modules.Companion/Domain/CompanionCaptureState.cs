namespace FinanceSentry.Modules.Companion.Domain;

/// <summary>
/// Per-source capture watermark (feature 031). Lets the poll read only rows newer than the last seen,
/// idempotently. Survives purges of <see cref="CompanionEvent"/> rows.
/// </summary>
public sealed class CompanionCaptureState
{
    /// <summary>Source key, e.g. <c>alerts</c> / <c>thesis-breaks</c> / <c>analyst-actions</c>.</summary>
    public string Source { get; set; } = string.Empty;

    public DateTimeOffset Watermark { get; set; }
}
