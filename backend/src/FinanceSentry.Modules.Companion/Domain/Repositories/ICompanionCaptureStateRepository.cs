namespace FinanceSentry.Modules.Companion.Domain.Repositories;

public interface ICompanionCaptureStateRepository
{
    /// <summary>The last-seen watermark for a source, or a floor (e.g. now minus a small window) if unset.</summary>
    Task<DateTimeOffset> GetWatermarkAsync(string source, DateTimeOffset floor, CancellationToken ct = default);

    Task SetWatermarkAsync(string source, DateTimeOffset watermark, CancellationToken ct = default);
}
