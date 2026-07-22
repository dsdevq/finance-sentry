namespace FinanceSentry.Modules.Companion.Application.Services;

/// <summary>
/// Polls the detectors (alerts, analyst actions on held names) for new material events since a
/// watermark, applies the materiality policy + dedup, and writes companion events with a disposition
/// derived from each user's current mode (feature 031, US2). Non-invasive: reads via Core contracts.
/// </summary>
public interface ICompanionEventCapture
{
    /// <summary>Runs one capture pass. Returns the number of new events written.</summary>
    Task<int> CaptureAsync(CancellationToken ct = default);
}
