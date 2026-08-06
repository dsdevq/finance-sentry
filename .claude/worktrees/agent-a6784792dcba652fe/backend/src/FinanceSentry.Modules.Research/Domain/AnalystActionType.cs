namespace FinanceSentry.Modules.Research.Domain;

/// <summary>The kind of street/analyst action recorded for a ticker (feature 030).</summary>
public enum AnalystActionType
{
    /// <summary>Rating raised (e.g. Equal-Weight → Overweight).</summary>
    Upgrade,

    /// <summary>Rating lowered.</summary>
    Downgrade,

    /// <summary>New coverage initiated.</summary>
    Initiate,

    /// <summary>Price target changed without a rating change.</summary>
    TargetChange,

    /// <summary>Rating/target reiterated.</summary>
    Reiterate,

    /// <summary>Named on a firm's top-ideas / conviction list.</summary>
    TopIdea,
}
