using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR.OAuth;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IbkrOAuthSignerTests
{
    // A real 512-bit DH parameter set (generator 2) — small enough for fast tests.
    private const string DhParamPem = """
        -----BEGIN DH PARAMETERS-----
        MEYCQQDTZI0rzokluVr08YUDnxIquTuVP2si5U72dzTZIoNCjMmta5I/kAzjvZsl
        sIfRMiH8ws2H0202+IpmImaUUXdfAgEC
        -----END DH PARAMETERS-----
        """;

    [Theory]
    [InlineData("abcXYZ019-_.~", "abcXYZ019-_.~")]
    [InlineData(" ", "%20")]
    [InlineData("a&b", "a%26b")]
    [InlineData("http://x.com", "http%3A%2F%2Fx.com")]
    [InlineData("=", "%3D")]
    public void PercentEncode_FollowsRfc3986(string input, string expected)
    {
        IbkrOAuthSigner.PercentEncode(input).Should().Be(expected);
    }

    [Fact]
    public void BuildBaseString_SortsParams_AndDoubleEncodes()
    {
        var parameters = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };

        var baseString = IbkrOAuthSigner.BuildBaseString("get", "http://x.com", parameters);

        baseString.Should().Be("GET&http%3A%2F%2Fx.com&a%3D1%26b%3D2");
    }

    [Fact]
    public void SignRsaSha256_ProducesVerifiableSignature()
    {
        using var rsa = RSA.Create(2048);
        const string data = "GET&url&params";

        var signature = IbkrOAuthSigner.SignRsaSha256(data, rsa);

        rsa.VerifyData(
            Encoding.UTF8.GetBytes(data),
            Convert.FromBase64String(signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1).Should().BeTrue();
    }

    [Fact]
    public void SignHmacSha256_MatchesRfc4231TestCase1()
    {
        var key = Enumerable.Repeat((byte)0x0b, 20).ToArray();
        const string expectedHex = "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7";

        var signature = IbkrOAuthSigner.SignHmacSha256("Hi There", key);

        Convert.ToHexStringLower(Convert.FromBase64String(signature)).Should().Be(expectedHex);
    }

    [Fact]
    public void DecryptAccessTokenSecret_RoundTripsThroughRsa()
    {
        using var rsa = RSA.Create(2048);
        var secret = "the-decrypted-secret"u8.ToArray();
        var encrypted = Convert.ToBase64String(rsa.Encrypt(secret, RSAEncryptionPadding.Pkcs1));

        var decrypted = IbkrOAuthSigner.DecryptAccessTokenSecret(encrypted, rsa);

        decrypted.Should().Equal(secret);
    }

    [Fact]
    public void ParseDhParams_ReadsPrimeAndGenerator()
    {
        var (prime, generator) = IbkrOAuthSigner.ParseDhParams(DhParamPem);

        generator.Should().Be(new BigInteger(2));
        prime.Should().BeGreaterThan(BigInteger.Zero);
    }

    [Fact]
    public void ComputeLiveSessionToken_DerivesSharedSecret_MatchingTheServerSide()
    {
        var (prime, generator) = IbkrOAuthSigner.ParseDhParams(DhParamPem);
        var secret = "access-token-secret-bytes"u8.ToArray();

        // Client generates its challenge A = g^a mod p.
        var (clientPrivate, challengeHex) = IbkrOAuthSigner.GenerateDhChallenge(prime, generator);
        var clientChallenge = new BigInteger(Convert.FromHexString(challengeHex), isUnsigned: true, isBigEndian: true);

        // Simulate the server: pick b, publish B = g^b mod p, derive K = A^b mod p.
        var serverPrivate = new BigInteger(RandomNumberGenerator.GetBytes(32), isUnsigned: true, isBigEndian: true);
        var serverChallenge = BigInteger.ModPow(generator, serverPrivate, prime);
        var serverShared = BigInteger.ModPow(clientChallenge, serverPrivate, prime);
        var expectedLst = ComputeExpectedLst(serverShared, secret);

        var lst = IbkrOAuthSigner.ComputeLiveSessionToken(
            prime, clientPrivate, ToHex(serverChallenge), secret);

        lst.Should().Be(expectedLst);
    }

    [Fact]
    public void ValidateLiveSessionToken_AcceptsMatchingSignature_RejectsOthers()
    {
        var lstBytes = RandomNumberGenerator.GetBytes(20);
        var lst = Convert.ToBase64String(lstBytes);
        const string consumerKey = "FINSENTRY";
        using var hmac = new HMACSHA1(lstBytes);
        var signatureHex = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(consumerKey)));

        IbkrOAuthSigner.ValidateLiveSessionToken(lst, consumerKey, signatureHex).Should().BeTrue();
        IbkrOAuthSigner.ValidateLiveSessionToken(lst, consumerKey, "deadbeef").Should().BeFalse();
    }

    private static string ComputeExpectedLst(BigInteger sharedSecret, byte[] secret)
    {
        var keyBytes = sharedSecret.ToByteArray(isUnsigned: false, isBigEndian: true);
        using var hmac = new HMACSHA1(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(secret));
    }

    private static string ToHex(BigInteger value) =>
        Convert.ToHexStringLower(value.ToByteArray(isUnsigned: true, isBigEndian: true));
}
