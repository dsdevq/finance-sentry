namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>The trading-day return/RS windows aligned with the momentum literature.</summary>
public static class StructureWindows
{
    public const int Month = 21;
    public const int Quarter = 63;
    public const int HalfYear = 126;
    public const int Year = 252;

    public static readonly IReadOnlyList<int> All = [Month, Quarter, HalfYear, Year];
}
