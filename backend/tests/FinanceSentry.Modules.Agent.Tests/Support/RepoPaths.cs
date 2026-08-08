namespace FinanceSentry.Modules.Agent.Tests.Support;

/// <summary>Locates the repo-root persona files by walking up from the test output directory.</summary>
public static class RepoPaths
{
    private const string Probe = "agent/ledger/persona.core.md";

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, Probe.Replace('/', Path.DirectorySeparatorChar))))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repo root (agent/ledger/persona.core.md).");
    }

    public static string ReadLedgerFile(string relative) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "agent", "ledger", relative.Replace('/', Path.DirectorySeparatorChar)));
}
