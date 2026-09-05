namespace FinanceSentry.Modules.Research.Domain.Ports;

/// <summary>
/// Cross-module port (039 pattern): runs the 040 in-app agent loop for a single prompt and returns
/// the assembled answer. The adapter lives in FinanceSentry.Integration so Research never
/// references the Agent module. Returns null when the agent produced no usable text.
/// </summary>
public interface ILedgerNarrator
{
    Task<string?> NarrateAsync(string prompt, CancellationToken ct = default);
}
