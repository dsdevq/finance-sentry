namespace FinanceSentry.Modules.Retention.Application.Services;

using System.Text.Json;
using FinanceSentry.Infrastructure.Observability;
using FinanceSentry.Modules.Retention.Domain;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Enforces every generic <see cref="RetentionAction.Purge"/> policy (feature 024, US1): batched,
/// idempotent hard-deletes of out-of-policy rows, with a <see cref="RetentionRun"/> record of what was
/// examined/removed. Cutoffs are UTC and far from <c>now()</c>, so concurrent sync writes are never in
/// range (no deadlock, no removing freshly-written rows). Identifiers come only from the compiled
/// registry and are quoted verbatim; the cutoff is a bound parameter — no injection surface.
/// </summary>
public sealed class RetentionPurgeService(
    RetentionDbContext db,
    IOptions<RetentionOptions> options,
    JobMetrics metrics,
    ILogger<RetentionPurgeService> logger)
{
    private readonly RetentionOptions _options = options.Value;

    /// <summary>Runs every generic purge policy. When <paramref name="dryRun"/>, counts but deletes nothing.</summary>
    public async Task<RetentionRun> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        var run = new RetentionRun { RunType = RetentionRunType.Purge, StartedAt = DateTimeOffset.UtcNow };
        var results = new List<TableResult>();
        var failures = 0;
        var policies = RetentionPolicyRegistry.GenericPurgePolicies.ToList();

        logger.LogInformation(
            "RetentionPurgeService starting. Policies: {Count}. DryRun: {DryRun}", policies.Count, dryRun);

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        foreach (var policy in policies)
        {
            var windowDays = ResolveWindow(policy);
            var cutoff = DateTimeOffset.UtcNow.AddDays(-windowDays);
            try
            {
                var examined = await CountAsync(connection, policy, cutoff, ct);
                long removed = 0;
                if (!dryRun && examined > 0)
                    removed = await PurgeBatchedAsync(connection, policy, cutoff, ct);

                results.Add(new TableResult(policy.QualifiedName, examined, removed));
                if (removed > 0)
                    metrics.RecordRetentionRowsRemoved(policy.QualifiedName, removed);

                logger.LogInformation(
                    "Retention {Table}: examined={Examined} removed={Removed} cutoff={Cutoff:yyyy-MM-dd} (window {Days}d)",
                    policy.QualifiedName, examined, removed, cutoff.UtcDateTime, windowDays);
            }
            catch (Exception ex)
            {
                failures++;
                results.Add(new TableResult(policy.QualifiedName, -1, 0));
                logger.LogError(ex, "Retention purge failed for {Table}", policy.QualifiedName);
            }
        }

        run.CompletedAt = DateTimeOffset.UtcNow;
        run.TableResults = JsonSerializer.Serialize(results);
        run.Outcome = failures == 0
            ? RetentionOutcome.Success
            : failures == policies.Count ? RetentionOutcome.Failed : RetentionOutcome.PartialSuccess;
        if (failures > 0)
            run.Error = $"{failures}/{policies.Count} policies failed; see logs.";

        db.RetentionRuns.Add(run);
        await db.SaveChangesAsync(ct);

        metrics.SetLastRetentionRun(RetentionRunType.Purge.ToString(), run.CompletedAt.Value);
        logger.LogInformation(
            "RetentionPurgeService completed. Outcome={Outcome} in {Ms}ms",
            run.Outcome, (run.CompletedAt.Value - run.StartedAt).TotalMilliseconds);
        return run;
    }

    private int ResolveWindow(RetentionPolicy policy) =>
        _options.WindowOverrides.TryGetValue(policy.QualifiedName, out var days) ? days : policy.WindowDays!.Value;

    private static async Task<long> CountAsync(
        NpgsqlConnection connection, RetentionPolicy policy, DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = RetentionSql.Count(policy);
        cmd.Parameters.AddWithValue("cutoff", cutoff);
        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar is long l ? l : 0;
    }

    private async Task<long> PurgeBatchedAsync(
        NpgsqlConnection connection, RetentionPolicy policy, DateTimeOffset cutoff, CancellationToken ct)
    {
        var batch = policy.BatchSize > 0 ? policy.BatchSize : _options.DefaultBatchSize;
        var sql = RetentionSql.PurgeBatch(policy, batch);

        long total = 0;
        int affected;
        do
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("cutoff", cutoff);
            affected = await cmd.ExecuteNonQueryAsync(ct);
            total += affected;
        }
        while (affected == batch && !ct.IsCancellationRequested);

        return total;
    }
}
