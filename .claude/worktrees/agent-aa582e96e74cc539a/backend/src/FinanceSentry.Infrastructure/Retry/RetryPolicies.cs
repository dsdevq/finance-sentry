namespace FinanceSentry.Infrastructure.Retry;

using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

/// <summary>
/// Polly retry policies for Plaid API calls.
///
/// FR-005: Max 3 attempts with exponential backoff delays: 5 min → 15 min → 1 hour.
/// Transient errors (timeout, rate limit, 5xx) are retried.
/// Permanent errors (auth failure 401/403, validation 400) fail immediately.
/// </summary>
public static class RetryPolicies
{
    /// <summary>Delay sequence per FR-005: 5 min, 15 min, 1 hour.</summary>
    public static readonly TimeSpan[] PlaidRetryDelays =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1)
    ];

    /// <summary>
    /// HTTP status codes that indicate a permanent failure — never retry these.
    /// </summary>
    private static readonly HashSet<HttpStatusCode> PermanentFailureCodes =
    [
        HttpStatusCode.BadRequest,          // 400 — validation error
        HttpStatusCode.Unauthorized,         // 401 — auth failure
        HttpStatusCode.Forbidden,            // 403 — auth failure
        HttpStatusCode.NotFound,             // 404 — resource not found
        HttpStatusCode.UnprocessableEntity,  // 422 — semantic validation error
    ];

    /// <summary>
    /// Builds the Plaid API retry policy: 3 attempts, FR-005 delays.
    /// Retries on transient errors (timeout, 429, 5xx).
    /// Fails immediately on permanent errors (400, 401, 403).
    /// Logs each retry attempt with correlation ID.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreatePlaidRetryPipeline(
        ILogger logger,
        string correlationId)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                DelayGenerator = static args =>
                {
                    var delay = args.AttemptNumber < PlaidRetryDelays.Length
                        ? PlaidRetryDelays[args.AttemptNumber]
                        : PlaidRetryDelays[^1];
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>() // timeout
                    .HandleResult(response =>
                        IsTransientHttpError(response.StatusCode)),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "[{CorrelationId}] Plaid API retry {Attempt}/{Max} after {Delay}. " +
                        "Outcome: {Outcome}",
                        correlationId,
                        args.AttemptNumber + 1,
                        3,
                        args.RetryDelay,
                        args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Simplified retry pipeline for operations that don't return HttpResponseMessage
    /// (e.g., database calls, internal service calls).
    /// Uses same 3-attempt / FR-005 delays pattern.
    /// </summary>
    public static ResiliencePipeline CreateTransientRetryPipeline(
        ILogger logger,
        string correlationId,
        int maxAttempts = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxAttempts,
                DelayGenerator = args =>
                {
                    var delay = args.AttemptNumber < PlaidRetryDelays.Length
                        ? PlaidRetryDelays[args.AttemptNumber]
                        : PlaidRetryDelays[^1];
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "[{CorrelationId}] Transient retry {Attempt}/{Max} after {Delay}: {Error}",
                        correlationId,
                        args.AttemptNumber + 1,
                        maxAttempts,
                        args.RetryDelay,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Returns true for HTTP status codes that indicate a transient (retryable) error.
    /// Returns false for permanent errors that should not be retried.
    /// </summary>
    public static bool IsTransientHttpError(HttpStatusCode statusCode)
    {
        if (PermanentFailureCodes.Contains(statusCode))
            return false;

        return statusCode == HttpStatusCode.TooManyRequests  // 429 rate limit
            || (int)statusCode >= 500;                       // 5xx server errors
    }

    /// <summary>
    /// Returns true if the status code represents a permanent failure that should
    /// immediately surface to the caller without retrying.
    /// </summary>
    public static bool IsPermanentFailure(HttpStatusCode statusCode)
        => PermanentFailureCodes.Contains(statusCode);
}
