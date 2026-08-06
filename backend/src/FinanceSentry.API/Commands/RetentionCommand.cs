namespace FinanceSentry.API.Commands;

using FinanceSentry.Modules.Retention.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// One-shot maintenance verbs for feature 024, so retention/backup jobs can be triggered out of band
/// (Hangfire "Trigger now" is behind dashboard auth). Not web endpoints — invoked via
/// <c>dotnet FinanceSentry.API.dll &lt;verb&gt;</c>:
///   <c>retention-purge [--dry-run]</c> · <c>db-backup</c> · <c>db-restore-verify</c>.
/// </summary>
public static class RetentionCommand
{
    public const string PurgeVerb = "retention-purge";
    public const string BackupVerb = "db-backup";
    public const string RestoreVerifyVerb = "db-restore-verify";

    public static bool Handles(string verb) =>
        verb is PurgeVerb or BackupVerb or RestoreVerifyVerb;

    public static async Task RunAsync(IServiceProvider services, string[] args)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var verb = args[0];

        switch (verb)
        {
            case PurgeVerb:
                var dryRun = args.Contains("--dry-run");
                Console.WriteLine($"[{PurgeVerb}] running (dryRun={dryRun})...");
                await sp.GetRequiredService<RetentionPurgeJob>().RunAsync(dryRun);
                break;

            case BackupVerb:
                Console.WriteLine($"[{BackupVerb}] running...");
                await sp.GetRequiredService<BackupJob>().RunAsync();
                break;

            case RestoreVerifyVerb:
                Console.WriteLine($"[{RestoreVerifyVerb}] running...");
                await sp.GetRequiredService<RestoreVerifyJob>().RunAsync();
                break;
        }

        Console.WriteLine($"[{verb}] done.");
    }
}
