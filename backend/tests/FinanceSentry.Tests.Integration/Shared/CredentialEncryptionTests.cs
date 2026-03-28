namespace FinanceSentry.Tests.Integration.Shared;

using FinanceSentry.Infrastructure.Encryption;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Integration tests for CredentialEncryptionService (T109).
///
/// These tests verify the full encrypt→store→retrieve→decrypt round-trip
/// without a live database (the encryption service is pure in-memory crypto).
/// Testcontainers PostgreSQL is reserved for repository-level integration tests in later phases.
/// </summary>
public class CredentialEncryptionTests
{
    // Two key versions to verify key rotation (T101 FR-003)
    private static readonly EncryptionOptions OptionsWithTwoKeys = new()
    {
        CurrentKeyVersion = 2,
        Keys = new Dictionary<int, string>
        {
            [1] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", // 32 zero-bytes base64
            [2] = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBA="  // 32 non-zero bytes base64
        }
    };

    private static IOptions<EncryptionOptions> Opts(EncryptionOptions opts)
        => Options.Create(opts);

    private static CredentialEncryptionService CreateSut(EncryptionOptions? opts = null)
        => new(Opts(opts ?? OptionsWithTwoKeys));

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var sut = CreateSut();
        const string plaintext = "access-token-abc123";

        var result = sut.Encrypt(plaintext);
        var decrypted = sut.Decrypt(result.Ciphertext, result.Iv, result.AuthTag, result.KeyVersion);

        decrypted.Should().Be(plaintext);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("a very long plaid access token that is several hundred characters long and includes special chars: ñ€£")]
    public void Encrypt_ThenDecrypt_RoundTrips_VariousLengths(string plaintext)
    {
        var sut = CreateSut();
        var result = sut.Encrypt(plaintext);
        sut.Decrypt(result.Ciphertext, result.Iv, result.AuthTag, result.KeyVersion).Should().Be(plaintext);
    }

    // ── Stored-blob simulation ──────────────────────────────────────────────

    [Fact]
    public void Decrypt_FromStoredBlobBytes_ReturnsPlaintext()
    {
        // Simulate writing to DB (serialise to byte arrays) then reading back
        var sut = CreateSut();
        const string originalToken = "plaid-access-prod-token-XXXX";

        var enc = sut.Encrypt(originalToken);

        // "Store" — copy to independent byte arrays (as a DB BYTEA round-trip would)
        var storedCiphertext = enc.Ciphertext.ToArray();
        var storedIv = enc.Iv.ToArray();
        var storedAuthTag = enc.AuthTag.ToArray();
        var storedVersion = enc.KeyVersion;

        // "Retrieve" — pass back copies
        var decrypted = sut.Decrypt(storedCiphertext, storedIv, storedAuthTag, storedVersion);

        decrypted.Should().Be(originalToken);
    }

    // ── Tamper detection ────────────────────────────────────────────────────

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var sut = CreateSut();
        var enc = sut.Encrypt("sensitive-token");

        enc.Ciphertext[0] ^= 0xFF; // flip all bits in first byte

        var act = () => sut.Decrypt(enc.Ciphertext, enc.Iv, enc.AuthTag, enc.KeyVersion);

        act.Should().Throw<System.Security.Cryptography.CryptographicException>(
            "AES-GCM auth tag verification must fail on ciphertext mutation");
    }

    [Fact]
    public void Decrypt_TamperedAuthTag_ThrowsCryptographicException()
    {
        var sut = CreateSut();
        var enc = sut.Encrypt("sensitive-token");

        enc.AuthTag[0] ^= 0x01;

        var act = () => sut.Decrypt(enc.Ciphertext, enc.Iv, enc.AuthTag, enc.KeyVersion);

        act.Should().Throw<System.Security.Cryptography.CryptographicException>(
            "AES-GCM must reject mutated auth tags");
    }

    // ── Key rotation ────────────────────────────────────────────────────────

    [Fact]
    public void Decrypt_WithOlderKeyVersion_SucceedsWhenKeyPresent()
    {
        // Encrypt with version 1 (older key), then decrypt — simulates post-rotation read
        var optsV1 = new EncryptionOptions
        {
            CurrentKeyVersion = 1, // encrypt with v1
            Keys = OptionsWithTwoKeys.Keys
        };

        var sut = CreateSut(optsV1);
        const string token = "legacy-access-token";

        var enc = sut.Encrypt(token);
        enc.KeyVersion.Should().Be(1);

        // Decrypt using the same service (which has both keys)
        var decrypted = sut.Decrypt(enc.Ciphertext, enc.Iv, enc.AuthTag, enc.KeyVersion);
        decrypted.Should().Be(token);
    }

    [Fact]
    public void Decrypt_WithMissingKeyVersion_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var enc = sut.Encrypt("token");

        // Request decryption with a key version not in the store
        var act = () => sut.Decrypt(enc.Ciphertext, enc.Iv, enc.AuthTag, keyVersion: 99);

        act.Should().Throw<InvalidOperationException>(
            "decrypting with an unknown key version must fail fast, not silently");
    }

    [Fact]
    public void Encrypt_UsesMostRecentKeyVersion()
    {
        var sut = CreateSut(); // CurrentKeyVersion = 2
        var enc = sut.Encrypt("token");

        enc.KeyVersion.Should().Be(2, "encryption must use the current key version");
    }

    // ── No plaintext leakage ─────────────────────────────────────────────────

    [Fact]
    public void EncryptionResult_DoesNotContainPlaintextBytes()
    {
        var sut = CreateSut();
        const string plaintext = "super-secret-plaid-token";
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);

        var enc = sut.Encrypt(plaintext);

        // The raw ciphertext must not be the same bytes as the plaintext
        enc.Ciphertext.Should().NotBeEquivalentTo(plaintextBytes,
            "ciphertext must differ from plaintext (AES-GCM encryption must be applied)");
    }
}
