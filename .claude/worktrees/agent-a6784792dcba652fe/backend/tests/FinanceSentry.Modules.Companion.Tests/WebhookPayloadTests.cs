namespace FinanceSentry.Modules.Companion.Tests;

using System.Net;
using FinanceSentry.Modules.Companion.Application.Services;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>Outbound wake payload — ids/refs only, no secrets/detail (feature 031, US2, T028).</summary>
public sealed class WebhookPayloadTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        public Uri? Uri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class FakeHttpFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private static CompanionEvent SampleEvent() => new()
    {
        Kind = CompanionEventKind.ThesisBreak,
        Subject = "MU",
        Severity = "critical",
        Summary = "SUPER-SECRET internal detail that must never leave FS",
        OccurredAt = DateTimeOffset.Parse("2026-07-22T10:00:00Z"),
    };

    [Fact]
    public async Task Configured_url_posts_ids_and_refs_only()
    {
        var handler = new CapturingHandler();
        var dispatcher = new WebhookAgentWakeDispatcher(
            new FakeHttpFactory(handler),
            Options.Create(new CompanionOptions { AgentTriggerUrl = "http://agent.local/trigger" }),
            NullLogger<WebhookAgentWakeDispatcher>.Instance);

        var evt = SampleEvent();
        var result = await dispatcher.WakeAsync(evt);

        result.Should().Be(WakeResult.Sent);
        handler.Uri!.ToString().Should().Be("http://agent.local/trigger");
        handler.Body.Should().Contain(evt.Id.ToString())
            .And.Contain("ThesisBreak").And.Contain("MU").And.Contain("critical").And.Contain("occurredAt");
        handler.Body.Should().NotContain("SUPER-SECRET", "the wake carries no full detail or secrets");
    }

    [Fact]
    public async Task No_url_is_not_configured_and_posts_nothing()
    {
        var handler = new CapturingHandler();
        var dispatcher = new WebhookAgentWakeDispatcher(
            new FakeHttpFactory(handler),
            Options.Create(new CompanionOptions { AgentTriggerUrl = null }),
            NullLogger<WebhookAgentWakeDispatcher>.Instance);

        var result = await dispatcher.WakeAsync(SampleEvent());

        result.Should().Be(WakeResult.NotConfigured);
        handler.Body.Should().BeNull();
    }
}
