namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Classifies bank-transfer transactions from their description prefix, for providers that
/// give no structured category (TrueLayer). Open-banking feeds render peer/account transfers
/// with a directional prefix — "To &lt;name/account&gt;" for outgoing, "From …" / "Payment from …"
/// for incoming — which is a reliable signal that a merchant-keyword match is not.
///
/// Deliberately conservative: only the directional prefixes match, and the trailing space is
/// required so "Tobacco"/"Tommy" don't trip the "To" branch. Merchant-keyword matching runs
/// first (see <see cref="CategoryResolver.ResolveDescription"/>), so "To Go Sushi" still lands
/// as food rather than a transfer.
/// </summary>
public static class TransferDescriptionClassifier
{
    private static readonly string[] OutgoingPrefixes = ["To "];
    private static readonly string[] IncomingPrefixes = ["From ", "Payment from "];

    /// <summary>
    /// Returns <see cref="CategoryKeys.TransferOut"/> / <see cref="CategoryKeys.TransferIn"/>
    /// for a directional-prefix description, or <c>null</c> when it is not a recognizable transfer.
    /// </summary>
    public static string? Resolve(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var d = description.TrimStart();

        foreach (var prefix in IncomingPrefixes)
        {
            if (d.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return CategoryKeys.TransferIn;
        }

        foreach (var prefix in OutgoingPrefixes)
        {
            if (d.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return CategoryKeys.TransferOut;
        }

        return null;
    }
}
