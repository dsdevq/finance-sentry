namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Maps raw provider instrument types and free-form IPS asset-class labels onto a
/// single canonical set of buckets, so actual holdings and IPS targets can be compared
/// on the same axis. v1 is intentionally coarse — ETFs/funds roll up to Equities; refine
/// as needed.
/// </summary>
public static class AssetClassNormalizer
{
    public const string Equities = "Equities";
    public const string Bonds = "Bonds";
    public const string Crypto = "Crypto";
    public const string Cash = "Cash";
    public const string RealEstate = "RealEstate";
    public const string Commodities = "Commodities";
    public const string Other = "Other";

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Other;
        }

        var v = raw.Trim().ToLowerInvariant();

        if (v.Contains("crypto") || v.Contains("coin") || v.Contains("token")) return Crypto;
        if (v.Contains("cash") || v.Contains("money market") || v == "mmf") return Cash;
        if (v.Contains("bond") || v.Contains("fixed") || v.Contains("treasur") || v.Contains("note") || v.Contains("bill")) return Bonds;
        if (v.Contains("reit") || v.Contains("real estate") || v.Contains("property")) return RealEstate;
        if (v.Contains("commodit") || v.Contains("gold") || v.Contains("metal") || v.Contains("future")) return Commodities;
        if (v.Contains("stock") || v.Contains("equit") || v.Contains("share") || v == "stk" || v.Contains("etf") || v.Contains("fund")) return Equities;

        return Other;
    }
}
