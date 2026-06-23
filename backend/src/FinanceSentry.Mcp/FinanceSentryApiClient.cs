namespace FinanceSentry.Mcp;

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

public sealed class FinanceSentryApiClient
{
    private const string DefaultApiBaseUrl = "http://localhost:5001/api/v1/";

    private readonly HttpClient _httpClient;
    private readonly FinanceSentryApiOptions _options;

    public FinanceSentryApiClient(HttpClient httpClient, IOptions<FinanceSentryApiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = BuildBaseAddress(_options.ApiBaseUrl);

        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        }
    }

    public async Task<JsonElement> GetJsonAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(NormalizePath(path), cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    public static JsonElement NotYetAvailable(string reason)
    {
        return JsonSerializer.SerializeToElement(new
        {
            status = "not_yet_available",
            reason,
        });
    }

    private static Uri BuildBaseAddress(string? configuredBaseUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? DefaultApiBaseUrl
            : configuredBaseUrl;

        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        return new Uri(baseUrl, UriKind.Absolute);
    }

    private static string NormalizePath(string path)
    {
        return path.TrimStart('/');
    }
}
