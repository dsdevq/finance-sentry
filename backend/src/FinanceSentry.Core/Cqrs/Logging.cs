using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Core.Cqrs;

public sealed class LoggingCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingCommandDecorator<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var name = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await inner.Handle(command, cancellationToken);
            logger.LogInformation("{Operation} handled in {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "{Operation} failed after {ElapsedMs}ms: {ErrorType}",
                name, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}

public sealed class LoggingQueryDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<LoggingQueryDecorator<TQuery, TResponse>> logger)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var name = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await inner.Handle(query, cancellationToken);
            logger.LogInformation("{Operation} handled in {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "{Operation} failed after {ElapsedMs}ms: {ErrorType}",
                name, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
