namespace FinanceSentry.Modules.Research.Domain;

// Canonical definition moved to FinanceSentry.Core.Domain.AssetClassNormalizer.
// This alias keeps existing code in this namespace compiling without changes.
public static class AssetClassNormalizer
{
    public const string Equities = Core.Domain.AssetClassNormalizer.Equities;
    public const string Bonds = Core.Domain.AssetClassNormalizer.Bonds;
    public const string Crypto = Core.Domain.AssetClassNormalizer.Crypto;
    public const string Cash = Core.Domain.AssetClassNormalizer.Cash;
    public const string RealEstate = Core.Domain.AssetClassNormalizer.RealEstate;
    public const string Commodities = Core.Domain.AssetClassNormalizer.Commodities;
    public const string Other = Core.Domain.AssetClassNormalizer.Other;

    public static string Normalize(string? raw) => Core.Domain.AssetClassNormalizer.Normalize(raw);
}
