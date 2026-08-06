namespace FinanceSentry.Tests.Unit.BankSync.Monobank;

using System.Net;
using System.Text;

/// <summary>
/// Test double for the Monobank REST API — records outgoing requests and replays
/// canned responses. No live HTTP. Each enqueued response answers one request in
/// order; the last enqueued response repeats for any further requests.
/// </summary>
internal sealed class MonobankStubHttpHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();
    private (HttpStatusCode Status, string Body)? _last;

    public List<string> RequestPaths { get; } = [];
    public List<string?> RequestTokens { get; } = [];

    public MonobankStubHttpHandler Enqueue(HttpStatusCode status, string body)
    {
        _responses.Enqueue((status, body));
        return this;
    }

    public FinanceSentry.Modules.BankSync.Infrastructure.Monobank.MonobankHttpClient BuildClient()
        => new(new HttpClient(this) { BaseAddress = new Uri("https://api.monobank.ua") });

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestPaths.Add(request.RequestUri!.AbsolutePath);
        RequestTokens.Add(request.Headers.TryGetValues("X-Token", out var values)
            ? values.FirstOrDefault()
            : null);

        if (_responses.Count > 0)
            _last = _responses.Dequeue();

        var (status, body) = _last ?? (HttpStatusCode.OK, "{}");
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
