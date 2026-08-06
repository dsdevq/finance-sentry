namespace FinanceSentry.Modules.BankSync.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Validates Plaid webhook HMAC-SHA256 signatures to prevent spoofed webhook calls.
/// </summary>
public interface IWebhookSignatureValidator
{
    /// <summary>
    /// Returns true when the HMAC-SHA256 of <paramref name="rawBody"/> keyed with
    /// <paramref name="webhookKey"/> matches <paramref name="signatureHeader"/>.
    /// Comparison is constant-time to resist timing attacks.
    /// </summary>
    bool IsValid(string rawBody, string signatureHeader, string webhookKey);
}

/// <inheritdoc />
public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    /// <inheritdoc />
    public bool IsValid(string rawBody, string signatureHeader, string webhookKey)
    {
        if (string.IsNullOrEmpty(rawBody) || string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(webhookKey))
            return false;

        var keyBytes  = Encoding.UTF8.GetBytes(webhookKey);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(keyBytes);
        var expectedBytes = hmac.ComputeHash(bodyBytes);
        var expectedHex   = Convert.ToHexString(expectedBytes).ToLowerInvariant();

        // Normalise the incoming header — Plaid sends a lowercase hex string.
        var incomingHex = signatureHeader.Trim().ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHex),
            Encoding.ASCII.GetBytes(incomingHex));
    }
}
