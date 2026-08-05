namespace FinanceSentry.Infrastructure.Observability.Hangfire;

using System.Net.Http;
using System.Net.Sockets;
using FinanceSentry.Infrastructure.Retry;

/// <summary>
/// Classifies a failed job's exception as transient/self-healing (US4) so throttling and network blips
/// don't count toward a consecutive-failure streak — mirroring the sync layer's transient set
/// (timeout / rate-limit / 5xx / network) so only genuine, sticky failures alert.
/// </summary>
public static class JobFailureTransientClassifier
{
    public static bool IsTransient(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or TaskCanceledException or OperationCanceledException or SocketException)
                return true;

            if (current is HttpRequestException http
                && http.StatusCode is { } status
                && RetryPolicies.IsTransientHttpError(status))
                return true;
        }

        return false;
    }
}
