namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Forwarding alias to <see cref="FinanceSentry.Core.Utils.AssetClassNormalizer"/>.
/// The canonical implementation moved to Core so Risk, Research, and MCP can all use it
/// without circular dependencies. New code should reference the Core type directly.
/// </summary>
public static class AssetClassNormalizer
{
    public const string Equities = Core.Utils.AssetClassNormalizer.Equities;
    public const string Bonds = Core.Utils.AssetClassNormalizer.Bonds;
    public const string Crypto = Core.Utils.AssetClassNormalizer.Crypto;
    public const string Cash = Core.Utils.AssetClassNormalizer.Cash;
    public const string RealEstate = Core.Utils.AssetClassNormalizer.RealEstate;
    public const string Commodities = Core.Utils.AssetClassNormalizer.Commodities;
    public const string Other = Core.Utils.AssetClassNormalizer.Other;

    public static string Normalize(string? raw)
        => Core.Utils.AssetClassNormalizer.Normalize(raw);
}
