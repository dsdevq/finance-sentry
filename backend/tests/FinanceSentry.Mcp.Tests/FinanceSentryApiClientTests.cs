namespace FinanceSentry.Mcp.Tests;

using System.Net;
using System.Text.Json;
using FinanceSentry.Mcp.Tools;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class FinanceSentryApiClientTests
{
    [Fact]
    public async Task GetAlerts_ShouldForwardToFinanceSentryApiWithBearerToken()
    {
        var handler = new RecordingHandler("""[{ "id": "alert-1", "status": "unread" }]""");
        using var httpClient = new HttpClient(handler);
        var apiClient = new FinanceSentryApiClient(
            httpClient,
            Options.Create(new FinanceSentryApiOptions
            {
                ApiBaseUrl = "https://finance-sentry.test/api/v1",
                ApiToken = "test-token",
            }));

        var tools = new AlertTools(apiClient);
        var result = await tools.GetAlerts("all", 1, 20, CancellationToken.None);

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Be("/api/v1/alerts?filter=all&page=1&pageSize=20");
        handler.Authorization.Should().Be("Bearer test-token");
        result.ValueKind.Should().Be(JsonValueKind.Array);
        result[0].GetProperty("id").GetString().Should().Be("alert-1");
    }

    private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody),
            });
        }
    }
}
