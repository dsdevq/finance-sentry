namespace FinanceSentry.API.Commands;

using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// One-shot maintenance command: runs the recurring subscription-detection job on demand
/// (the same job Hangfire schedules daily). Useful after a detection-logic change or a
/// data backfill, since Hangfire here uses in-memory storage and can't be triggered out of band.
///
/// Invoked via <c>dotnet FinanceSentry.API.dll detect-subscriptions</c> — not a web endpoint.
/// </summary>
public static class SubscriptionDetectionCommand
{
    public const string Verb = "detect-subscriptions";

    public static async Task RunAsync(IServiceProvider services, string[] args)
    {
        using var scope = services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<SubscriptionDetectionJob>();

        Console.WriteLine("[detect-subscriptions] running...");
        await job.ExecuteAsync();
        Console.WriteLine("[detect-subscriptions] done.");
    }
}
