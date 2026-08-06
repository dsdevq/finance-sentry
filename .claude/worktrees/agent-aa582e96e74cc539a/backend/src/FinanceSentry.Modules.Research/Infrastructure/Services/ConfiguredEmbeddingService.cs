namespace FinanceSentry.Modules.Research.Infrastructure.Services;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceSentry.Modules.Research.Application.Services;
using Microsoft.Extensions.Options;

/// <summary>
/// Embeddings via a configured OpenAI-compatible endpoint (POST {BaseUrl}/embeddings). Provider,
/// model, and credentials are deploy-time configuration; nothing provider-specific leaks into
/// domain or MCP code.
/// </summary>
public sealed class ConfiguredEmbeddingService(
    IHttpClientFactory httpFactory,
    IOptions<ResearchRetrievalOptions> options) : IEmbeddingService
{
    public const string HttpClientName = "research-embeddings";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private ResearchRetrievalOptions.EmbeddingProviderOptions Config => options.Value.Embedding;

    public bool IsEnabled => Config.Enabled && !string.IsNullOrWhiteSpace(Config.ApiKey);

    public string Provider => Config.Provider;

    public string Model => Config.Model;

    public int Dimensions => Config.Dimensions;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        if (!IsEnabled)
        {
            throw new InvalidOperationException("Embedding provider is not configured.");
        }

        var client = httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{Config.BaseUrl.TrimEnd('/')}/embeddings")
        {
            Content = JsonContent.Create(new EmbeddingRequest(Config.Model, texts), options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Config.ApiKey);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Embedding provider returned an empty response.");
        if (payload.Data.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"Embedding provider returned {payload.Data.Count} vectors for {texts.Count} inputs.");
        }

        return payload.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();
    }

    private sealed record EmbeddingRequest(string Model, IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse([property: JsonPropertyName("data")] List<EmbeddingDatum> Data);

    private sealed record EmbeddingDatum(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
