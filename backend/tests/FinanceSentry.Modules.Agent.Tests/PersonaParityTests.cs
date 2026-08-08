namespace FinanceSentry.Modules.Agent.Tests;

using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// US3 parity guard: both runtimes compose the same core, so the shared operating laws appear in each;
/// but only the OpenClaw compose carries OpenClaw-only mechanics — proving the runtime split holds.
/// </summary>
public sealed class PersonaParityTests
{
    private static readonly string[] SharedCoreLaws =
    [
        "materiality",
        "Tier-3 line",
        "Stay-invested",
        "Three-layer",
    ];

    private static string BrowserPrompt() =>
        new PersonaComposer(
            Options.Create(new AgentOptions { PersonaRootPath = RepoPaths.RepoRoot() }),
            NullLogger<PersonaComposer>.Instance).Compose();

    private static string OpenClawPrompt() => string.Join(
        "\n\n---\n\n",
        RepoPaths.ReadLedgerFile("persona.core.md").Trim(),
        RepoPaths.ReadLedgerFile("adapters/openclaw.md").Trim(),
        RepoPaths.ReadLedgerFile("user.md").Trim());

    [Fact]
    public void BothComposes_ShareTheCoreLaws()
    {
        var browser = BrowserPrompt();
        var openclaw = OpenClawPrompt();

        foreach (var law in SharedCoreLaws)
        {
            browser.Should().Contain(law, $"the browser persona must carry the shared law '{law}'");
            openclaw.Should().Contain(law, $"the OpenClaw persona must carry the shared law '{law}'");
        }
    }

    [Fact]
    public void OnlyOpenClaw_CarriesOpenClawMechanics()
    {
        var browser = BrowserPrompt();
        var openclaw = OpenClawPrompt();

        openclaw.Should().Contain("A2A envelope");
        openclaw.Should().Contain("Kit routes tasks");

        browser.Should().NotContain("A2A envelope");
        browser.Should().NotContain("Kit routes tasks");
    }
}
