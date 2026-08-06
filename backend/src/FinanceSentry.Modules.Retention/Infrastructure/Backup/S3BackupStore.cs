namespace FinanceSentry.Modules.Retention.Infrastructure.Backup;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Domain;
using Microsoft.Extensions.Options;

/// <summary>
/// <see cref="IBackupStore"/> backed by Cloudflare R2 via the S3-compatible API (feature 024, US2).
/// R2 speaks S3 with path-style addressing and an <c>auto</c> region. Artifacts are already
/// age-encrypted before they reach here, so R2's own at-rest encryption is defence-in-depth.
/// </summary>
public sealed class S3BackupStore : IBackupStore, IDisposable
{
    private readonly BackupOptions _options;
    private readonly IAmazonS3 _client;

    public S3BackupStore(IOptions<BackupOptions> options)
    {
        _options = options.Value;
        var config = new AmazonS3Config
        {
            ServiceURL = _options.R2Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        };
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.R2AccessKey, _options.R2SecretKey), config);
    }

    public async Task PutAsync(string key, Stream content, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.R2Bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // R2 does not support streaming SigV4 payload signing.
        };
        await _client.PutObjectAsync(request, ct);
    }

    public async Task DownloadToFileAsync(string key, string destinationPath, CancellationToken ct)
    {
        using var response = await _client.GetObjectAsync(_options.R2Bucket, key, ct);
        await response.WriteResponseStreamToFileAsync(destinationPath, append: false, ct);
    }

    public async Task<IReadOnlyList<BackupObject>> ListAsync(string prefix, CancellationToken ct)
    {
        var results = new List<BackupObject>();
        string? continuationToken = null;
        do
        {
            var response = await _client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _options.R2Bucket,
                    Prefix = prefix,
                    ContinuationToken = continuationToken,
                },
                ct);

            // AWSSDK v4 returns a null S3Objects list (not empty) when the prefix has no objects.
            foreach (var o in response.S3Objects ?? [])
                results.Add(new BackupObject(o.Key, o.Size ?? 0, o.LastModified ?? default));

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return results.OrderByDescending(o => o.LastModified).ToList();
    }

    public Task DeleteAsync(string key, CancellationToken ct) =>
        _client.DeleteObjectAsync(_options.R2Bucket, key, ct);

    public void Dispose() => _client.Dispose();
}
