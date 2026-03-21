namespace FinanceSentry.Infrastructure;

/// <summary>
/// Provides access to correlation ID for distributed tracing.
/// </summary>
public interface ICorrelationIdAccessor
{
    string GetCorrelationId();
}

/// <summary>
/// Implementation of ICorrelationIdAccessor using AsyncLocal for context-specific storage.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> CorrelationIdAsyncLocal = new();

    public string GetCorrelationId()
    {
        return CorrelationIdAsyncLocal.Value ?? Guid.NewGuid().ToString();
    }

    public void SetCorrelationId(string correlationId)
    {
        CorrelationIdAsyncLocal.Value = correlationId;
    }
}
