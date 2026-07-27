namespace FinanceSentry.Modules.Research.Application.Services;

using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.Research.Domain;
using Microsoft.Extensions.Options;

public sealed class ResearchChunker(IOptions<ResearchRetrievalOptions> options) : IResearchChunker
{
    private const int EstimatedCharsPerToken = 4;

    public IReadOnlyList<ResearchChunk> Chunk(ResearchDocument document)
    {
        var text = NormalizeText(document.Text);
        if (text.Length == 0)
        {
            return [];
        }

        var opts = options.Value;
        var chunkSize = Math.Max(1, opts.ChunkSizeChars);
        var overlap = Math.Clamp(opts.ChunkOverlapChars, 0, chunkSize - 1);
        var chunks = new List<ResearchChunk>();
        var position = 0;
        var ordinal = 0;

        while (position < text.Length && ordinal < opts.MaxChunksPerDocument)
        {
            var end = Math.Min(position + chunkSize, text.Length);
            if (end < text.Length)
            {
                var breakAt = text.LastIndexOfAny([' ', '\n'], end - 1, end - position);
                if (breakAt > position)
                {
                    end = breakAt + 1;
                }
            }

            var chunkText = text[position..end].Trim();
            if (chunkText.Length > 0)
            {
                chunks.Add(new ResearchChunk
                {
                    DocumentId = document.Id,
                    Ordinal = ordinal,
                    Text = chunkText,
                    ContentHash = ComputeContentHash(chunkText),
                    TokenEstimate = (chunkText.Length + EstimatedCharsPerToken - 1) / EstimatedCharsPerToken,
                    StartOffset = position,
                    EndOffset = end,
                });
                ordinal++;
            }

            if (end >= text.Length)
            {
                break;
            }

            position = Math.Max(end - overlap, position + 1);
        }

        return chunks;
    }

    public static string ComputeContentHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeText(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
}
