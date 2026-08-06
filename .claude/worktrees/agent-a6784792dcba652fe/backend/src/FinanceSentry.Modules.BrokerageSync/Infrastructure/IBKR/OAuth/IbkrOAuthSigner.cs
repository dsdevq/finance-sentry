using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR.OAuth;

/// <summary>
/// Deterministic OAuth 1.0a crypto for IBKR's Web API, isolated from any HTTP or
/// state so every step is independently unit-testable.
///
/// IBKR's flavour is signature-based, not bearer-based:
/// <list type="number">
///   <item>The access token secret is delivered RSA-encrypted; decrypt it with
///     the user's private <c>encryption</c> key to get the "prepend".</item>
///   <item>The live-session-token (LST) request is signed <c>RSA-SHA256</c> with
///     the user's private <c>signature</c> key over (prepend + OAuth base
///     string), carrying a Diffie-Hellman challenge.</item>
///   <item>The LST itself is <em>derived</em> — <c>HMAC-SHA1(K, secret)</c> where
///     K is the DH shared secret — never transmitted.</item>
///   <item>Every subsequent API request is signed <c>HMAC-SHA256</c> with the
///     base64-decoded LST as the key.</item>
/// </list>
/// </summary>
public static class IbkrOAuthSigner
{
    /// <summary>RFC 3986 percent-encoding: only unreserved characters pass through.</summary>
    public static string PercentEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
        {
            var isUnreserved =
                (b >= 'A' && b <= 'Z') ||
                (b >= 'a' && b <= 'z') ||
                (b >= '0' && b <= '9') ||
                b == '-' || b == '_' || b == '.' || b == '~';
            if (isUnreserved)
                sb.Append((char)b);
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds the standard OAuth 1.0 signature base string:
    /// <c>METHOD&amp;pctenc(url)&amp;pctenc(sorted &amp;-joined params)</c>.
    /// </summary>
    public static string BuildBaseString(
        string httpMethod,
        string url,
        IReadOnlyDictionary<string, string> parameters)
    {
        var normalized = parameters
            .Select(p => (Key: PercentEncode(p.Key), Value: PercentEncode(p.Value)))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ThenBy(p => p.Value, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}");
        var joined = string.Join("&", normalized);
        return $"{httpMethod.ToUpperInvariant()}&{PercentEncode(url)}&{PercentEncode(joined)}";
    }

    /// <summary>RSA-SHA256 signature (PKCS#1 v1.5), base64-encoded — used for the LST request.</summary>
    public static string SignRsaSha256(string data, RSA signatureKey)
    {
        var signature = signatureKey.SignData(
            Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>HMAC-SHA256 signature keyed by the base64-decoded LST, base64-encoded — used for API calls.</summary>
    public static string SignHmacSha256(string data, byte[] liveSessionTokenBytes)
    {
        using var hmac = new HMACSHA256(liveSessionTokenBytes);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    /// <summary>
    /// RSA-decrypts the access token secret (PKCS#1 v1.5) with the private
    /// encryption key. The returned bytes are both the HMAC message for the LST
    /// and — as lowercase hex — the base-string prepend.
    /// </summary>
    public static byte[] DecryptAccessTokenSecret(string accessTokenSecretBase64, RSA encryptionKey)
        => encryptionKey.Decrypt(Convert.FromBase64String(accessTokenSecretBase64), RSAEncryptionPadding.Pkcs1);

    /// <summary>Parses a <c>DH PARAMETERS</c> PEM into its prime (p) and generator (g).</summary>
    public static (BigInteger Prime, BigInteger Generator) ParseDhParams(string dhParamPem)
    {
        var der = DecodePem(dhParamPem);
        var sequence = new AsnReader(der, AsnEncodingRules.DER).ReadSequence();
        var prime = sequence.ReadInteger();
        var generator = sequence.ReadInteger();
        return (prime, generator);
    }

    /// <summary>
    /// Generates the DH challenge <c>A = g^a mod p</c> for a fresh random <c>a</c>.
    /// Returns <c>a</c> (needed later to derive the shared secret) and the
    /// challenge as unsigned big-endian hex.
    /// </summary>
    public static (BigInteger PrivateValue, string ChallengeHex) GenerateDhChallenge(
        BigInteger prime, BigInteger generator)
    {
        var privateValue = RandomPositiveBigInteger(byteLength: 32);
        var challenge = BigInteger.ModPow(generator, privateValue, prime);
        return (privateValue, ToHex(challenge));
    }

    /// <summary>
    /// Derives the live session token: shared secret <c>K = B^a mod p</c>, then
    /// <c>base64(HMAC-SHA1(K, decryptedSecret))</c>.
    /// </summary>
    public static string ComputeLiveSessionToken(
        BigInteger prime,
        BigInteger privateValue,
        string dhResponseHex,
        byte[] decryptedSecret)
    {
        var serverChallenge = ParseHex(dhResponseHex);
        var sharedSecret = BigInteger.ModPow(serverChallenge, privateValue, prime);
        var keyBytes = ToSignedBigEndianBytes(sharedSecret);
        using var hmac = new HMACSHA1(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(decryptedSecret));
    }

    /// <summary>
    /// Confirms the LST matches IBKR's returned signature:
    /// <c>hex(HMAC-SHA1(LST, consumerKey)) == liveSessionTokenSignature</c>.
    /// </summary>
    public static bool ValidateLiveSessionToken(
        string liveSessionTokenBase64,
        string consumerKey,
        string liveSessionTokenSignatureHex)
    {
        var lstBytes = Convert.FromBase64String(liveSessionTokenBase64);
        using var hmac = new HMACSHA1(lstBytes);
        var computed = ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(consumerKey)));
        return string.Equals(computed, liveSessionTokenSignatureHex, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Lowercase hex of a byte array.</summary>
    public static string ToHex(byte[] bytes) => Convert.ToHexStringLower(bytes);

    private static string ToHex(BigInteger value) => ToHex(value.ToByteArray(isUnsigned: true, isBigEndian: true));

    private static BigInteger ParseHex(string hex) =>
        new(Convert.FromHexString(hex.Length % 2 == 0 ? hex : "0" + hex), isUnsigned: true, isBigEndian: true);

    // Positive big-endian with a leading 0x00 whenever the top bit is set, so the
    // value stays unsigned — this is the byte form IBKR keys HMAC-SHA1 with.
    private static byte[] ToSignedBigEndianBytes(BigInteger value) =>
        value.ToByteArray(isUnsigned: false, isBigEndian: true);

    private static BigInteger RandomPositiveBigInteger(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    private static byte[] DecodePem(string pem)
    {
        var body = new StringBuilder();
        foreach (var line in pem.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("-----", StringComparison.Ordinal))
                continue;
            body.Append(trimmed);
        }
        return Convert.FromBase64String(body.ToString());
    }
}
