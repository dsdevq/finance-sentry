namespace FinanceSentry.Modules.Research.Domain;

/// <summary>Why a ticker is in the analyst-actions ingestion universe (feature 030).</summary>
public enum UniverseReason
{
    /// <summary>Member of the checked-in large-cap index seed list.</summary>
    IndexConstituent,

    /// <summary>Held in a brokerage account.</summary>
    Holding,

    /// <summary>On the watchlist.</summary>
    Watchlist,

    /// <summary>An open opportunity candidate.</summary>
    Candidate,

    /// <summary>Manually added.</summary>
    Manual,
}
