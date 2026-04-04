namespace FinanceSentry.Modules.BankSync.Infrastructure.Performance;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// EF Core DbCommandInterceptor that logs slow queries and warns on patterns
/// that may indicate N+1 problems (many sequential round-trips).
/// Slow threshold: WARNING at >100ms, ERROR at >500ms.
/// </summary>
public sealed class EFQueryLoggerInterceptor : DbCommandInterceptor
{
    private const int WarnThresholdMs = 100;
    private const int ErrorThresholdMs = 500;

    private readonly ILogger<EFQueryLoggerInterceptor> _logger;

    // Per-request round-trip counter (thread-local, lightweight approximation)
    [ThreadStatic]
    private static int _roundTripsThisRequest;

    public EFQueryLoggerInterceptor(ILogger<EFQueryLoggerInterceptor> logger)
    {
        _logger = logger;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogIfSlow(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration)
    {
        _roundTripsThisRequest++;
        var ms = (long)duration.TotalMilliseconds;

        if (ms >= ErrorThresholdMs)
        {
            _logger.LogError(
                "SLOW QUERY [{Ms}ms] Round-trips this request: {RoundTrips}. SQL: {Sql}",
                ms, _roundTripsThisRequest, TruncateSql(command.CommandText));
        }
        else if (ms >= WarnThresholdMs)
        {
            _logger.LogWarning(
                "Slow query [{Ms}ms] SQL: {Sql}",
                ms, TruncateSql(command.CommandText));
        }

        if (_roundTripsThisRequest > 10)
        {
            _logger.LogWarning(
                "Potential N+1 detected: {Count} DB round-trips in a single request.",
                _roundTripsThisRequest);
        }
    }

    private static string TruncateSql(string sql) =>
        sql.Length > 500 ? sql[..500] + "…" : sql;
}
