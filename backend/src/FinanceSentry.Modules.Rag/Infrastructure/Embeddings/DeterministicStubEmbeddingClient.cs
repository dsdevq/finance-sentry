namespace FinanceSentry.Modules.Rag.Infrastructure.Embeddings;

using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.Rag.Domain;

/// <summary>
/// Deterministic stub: same input always returns the same L2-normalised 1024-dim vector
/// derived from the SHA-256 hash of the input text. Intended for tests and cold-start
/// environments before a real embedding model is wired in.
/// </summary>
public sealed class DeterministicStubEmbeddingClient : IEmbeddingClient
{
    private const int Dimensions = 1024;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        => Task.FromResult(Generate(text));

    public static float[] Generate(string text)
    {
        var seed = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));

        // Expand 32-byte seed to Dimensions uint values by hashing [seed || counter].
        // Use uint → float mapping to avoid IEEE 754 NaN/Inf bit patterns.
        var counterBuffer = new byte[4];
        var hashInput = new byte[seed.Length + counterBuffer.Length];
        seed.CopyTo(hashInput, 0);

        var uintBytes = new byte[Dimensions * sizeof(uint)];
        var bytesWritten = 0;
        for (var counter = 0; bytesWritten < uintBytes.Length; counter++)
        {
            BitConverter.TryWriteBytes(counterBuffer, counter);
            counterBuffer.CopyTo(hashInput, seed.Length);
            var chunk = SHA256.HashData(hashInput);
            var copy = Math.Min(chunk.Length, uintBytes.Length - bytesWritten);
            chunk.AsSpan(0, copy).CopyTo(uintBytes.AsSpan(bytesWritten));
            bytesWritten += copy;
        }

        // Map each uint to [-1, 1] — avoids all NaN/Inf values that BitConverter.ToSingle may produce.
        var raw = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
        {
            var u = BitConverter.ToUInt32(uintBytes, i * sizeof(uint));
            raw[i] = (float)u / (float)uint.MaxValue * 2f - 1f;
        }

        return Normalize(raw);
    }

    private static float[] Normalize(float[] v)
    {
        var norm = 0f;
        foreach (var x in v) norm += x * x;
        norm = MathF.Sqrt(norm);
        if (norm <= 0f) return v;
        for (var i = 0; i < v.Length; i++) v[i] /= norm;
        return v;
    }
}
