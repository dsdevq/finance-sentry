namespace FinanceSentry.Modules.Radar.Domain;

/// <summary>Well-known scanner + signal-type keys for the shared radar_signals log.</summary>
public static class RadarScanners
{
    public const string MarketStructure = "market_structure";
}

public static class RadarSignalTypes
{
    public const string RotationShift = "rotation_shift";
    public const string HeldSectorLaggard = "held_sector_laggard";
    public const string Breadth = "breadth";
    public const string UnusualMove = "unusual_move";
    public const string Extended = "extended";
}

public static class RadarSubjectTypes
{
    public const string Ticker = "Ticker";
    public const string Sector = "Sector";
    public const string Universe = "Universe";
}
