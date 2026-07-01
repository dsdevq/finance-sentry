namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Spawns, stops, and inspects per-user IBeam containers via the Docker daemon.
/// One container per user, named after the user's IBKR credential id so the
/// gateway resolver can address it deterministically.
/// </summary>
public interface IIBeamContainerManager
{
    /// <summary>
    /// Ensures a running IBeam container exists for the given credential id
    /// with the supplied IBKR username and password. Idempotent: if a
    /// container with the target name already exists it is removed first so
    /// this call always produces a fresh session.
    /// </summary>
    Task SpawnAsync(Guid credentialId, string ibkrUsername, string ibkrPassword, CancellationToken ct = default);

    /// <summary>
    /// Stops and removes the user's IBeam container if it exists. No-op when
    /// the container is already gone.
    /// </summary>
    Task StopAndRemoveAsync(Guid credentialId, CancellationToken ct = default);

    /// <summary>
    /// Waits until the user's IBeam container reports an authenticated session
    /// (auth/status returns <c>authenticated=true</c>) or the configured
    /// timeout is reached. Returns true on success, false on timeout.
    /// </summary>
    Task<bool> WaitForAuthAsync(Guid credentialId, CancellationToken ct = default);
}
