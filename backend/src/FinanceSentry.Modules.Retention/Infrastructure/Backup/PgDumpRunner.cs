namespace FinanceSentry.Modules.Retention.Infrastructure.Backup;

using System.Diagnostics;
using FinanceSentry.Modules.Retention.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Shells the PostgreSQL client tools + <c>age</c> to create encrypted dumps and to restore them into an
/// isolated scratch database (feature 024, US2). Connection parameters and the age identity are passed as
/// process environment (never on the command line), so secrets never appear in argv or error output.
/// Each step is a discrete process with checked exit codes — no shell pipe whose failure could be masked.
/// </summary>
public sealed class PgDumpRunner
{
    private readonly NpgsqlConnectionStringBuilder _conn;
    private readonly BackupOptions _options;

    public PgDumpRunner(IConfiguration config, IOptions<BackupOptions> options)
    {
        var cs = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required for backups.");
        _conn = new NpgsqlConnectionStringBuilder(cs);
        _options = options.Value;
    }

    /// <summary>Custom-format <c>pg_dump</c> of the app database, age-encrypted to <paramref name="encryptedOutPath"/>.</summary>
    public async Task DumpAndEncryptAsync(string encryptedOutPath, CancellationToken ct)
    {
        var plainDump = encryptedOutPath + ".tmp";
        try
        {
            await RunAsync("pg_dump", ["-Fc", "-f", plainDump], DbEnv(_conn.Database!), ct);
            await RunAsync("age", ["-r", _options.AgeRecipient!, "-o", encryptedOutPath, plainDump], null, ct);
        }
        finally
        {
            TryDelete(plainDump);
        }
    }

    /// <summary>Decrypts <paramref name="encryptedPath"/> and restores it into <paramref name="targetDb"/>.</summary>
    public async Task DecryptAndRestoreAsync(string encryptedPath, string targetDb, CancellationToken ct)
    {
        var identityFile = await WriteIdentityFileAsync(ct);
        var plainDump = encryptedPath + ".restore.tmp";
        try
        {
            await RunAsync("age", ["-d", "-i", identityFile, "-o", plainDump, encryptedPath], null, ct);
            await RunAsync(
                "pg_restore",
                ["--no-owner", "--no-privileges", "-d", targetDb, plainDump],
                DbEnv(targetDb),
                ct);
        }
        finally
        {
            TryDelete(plainDump);
            TryDelete(identityFile);
        }
    }

    /// <summary>Creates an empty database (connects to the <c>postgres</c> maintenance DB).</summary>
    public Task CreateDatabaseAsync(string name, CancellationToken ct) =>
        RunAsync("createdb", [name], DbEnv("postgres"), ct);

    /// <summary>Drops a database if it exists (connects to the <c>postgres</c> maintenance DB).</summary>
    public Task DropDatabaseAsync(string name, CancellationToken ct) =>
        RunAsync("dropdb", ["--if-exists", name], DbEnv("postgres"), ct);

    /// <summary>Connection string for verification reads against a restored scratch database.</summary>
    public string ScratchConnectionString(string scratchDb) =>
        new NpgsqlConnectionStringBuilder(_conn.ConnectionString) { Database = scratchDb }.ConnectionString;

    private Dictionary<string, string> DbEnv(string database) => new()
    {
        ["PGHOST"] = _conn.Host ?? "localhost",
        ["PGPORT"] = (_conn.Port == 0 ? 5432 : _conn.Port).ToString(),
        ["PGUSER"] = _conn.Username ?? string.Empty,
        ["PGPASSWORD"] = _conn.Password ?? string.Empty,
        ["PGDATABASE"] = database,
    };

    private async Task<string> WriteIdentityFileAsync(CancellationToken ct)
    {
        var path = Path.Combine(Path.GetTempPath(), $"backup-age-{Guid.NewGuid():N}.key");
        await File.WriteAllTextAsync(path, _options.AgeIdentity ?? string.Empty, ct);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return path;
    }

    private static async Task RunAsync(
        string fileName, string[] args, IReadOnlyDictionary<string, string>? env, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        if (env is not null)
            foreach (var (k, v) in env)
                psi.Environment[k] = v;

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{fileName} exited {process.ExitCode}: {stderr.Trim()}");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }
}
