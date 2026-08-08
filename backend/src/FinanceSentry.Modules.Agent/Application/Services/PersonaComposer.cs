namespace FinanceSentry.Modules.Agent.Application.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Composes the browser Ledger's system prompt from the versioned persona-as-code files (US1):
/// <c>agent/ledger/persona.core.md</c> + <c>adapters/browser.md</c> + <c>user.md</c>. The composed
/// prompt is cached; editing the core changes both runtimes (the parity guarantee, US3). The persona
/// files live at the repo root, outside <c>backend/</c>, so the path is resolved robustly for both the
/// container (linked into the publish output) and dev/test (walk up to the repo root).
/// </summary>
public sealed class PersonaComposer(IOptions<AgentOptions> options, ILogger<PersonaComposer> logger)
{
    private static readonly string[] RelativeFiles =
    [
        Path.Combine("agent", "ledger", "persona.core.md"),
        Path.Combine("agent", "ledger", "adapters", "browser.md"),
        Path.Combine("agent", "ledger", "user.md"),
    ];

    private const string CoreProbe = "agent/ledger/persona.core.md";

    private readonly AgentOptions _options = options.Value;
    private readonly ILogger<PersonaComposer> _logger = logger;
    private readonly Lock _sync = new();
    private string? _cached;

    /// <summary>Returns the composed system prompt, reading + caching on first call.</summary>
    public string Compose()
    {
        lock (_sync)
        {
            if (_cached is not null)
            {
                return _cached;
            }

            var root = ResolvePersonaRoot()
                ?? throw new InvalidOperationException(
                    "Could not locate the persona files (agent/ledger/*.md). Set Agent:PersonaRootPath or ship the files with the app.");

            var sections = new List<string>(RelativeFiles.Length);
            foreach (var relative in RelativeFiles)
            {
                var path = Path.Combine(root, relative);
                sections.Add(File.ReadAllText(path).Trim());
            }

            _cached = string.Join("\n\n---\n\n", sections);
            _logger.LogInformation("Composed Ledger browser persona ({Length} chars) from {Root}.", _cached.Length, root);
            return _cached;
        }
    }

    private string? ResolvePersonaRoot()
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.PersonaRootPath))
        {
            candidates.Add(_options.PersonaRootPath!);
        }

        candidates.Add(AppContext.BaseDirectory);
        candidates.Add(Directory.GetCurrentDirectory());

        foreach (var start in candidates)
        {
            var found = ProbeUpwards(start);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string? ProbeUpwards(string start)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(start));
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, CoreProbe.Replace('/', Path.DirectorySeparatorChar))))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
