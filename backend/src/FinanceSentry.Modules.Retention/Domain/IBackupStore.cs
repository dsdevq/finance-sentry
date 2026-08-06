namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>
/// Off-host object store for encrypted backup artifacts (feature 024, US2). A domain interface so no
/// module code references the concrete AWS SDK directly (Principle I); the R2 implementation lives in
/// Infrastructure.
/// </summary>
public interface IBackupStore
{
    /// <summary>Uploads an artifact under <paramref name="key"/> (e.g. <c>daily/2026-08-06T02-00-00Z.dump.age</c>).</summary>
    Task PutAsync(string key, Stream content, CancellationToken ct);

    /// <summary>Downloads an artifact to a local file path.</summary>
    Task DownloadToFileAsync(string key, string destinationPath, CancellationToken ct);

    /// <summary>Lists artifacts under a key prefix (e.g. <c>daily/</c>), newest first.</summary>
    Task<IReadOnlyList<BackupObject>> ListAsync(string prefix, CancellationToken ct);

    /// <summary>Deletes an artifact.</summary>
    Task DeleteAsync(string key, CancellationToken ct);
}

/// <summary>A stored artifact's metadata.</summary>
/// <param name="Key">Object key.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="LastModified">Store-reported last-modified time.</param>
public sealed record BackupObject(string Key, long Size, DateTimeOffset LastModified);
