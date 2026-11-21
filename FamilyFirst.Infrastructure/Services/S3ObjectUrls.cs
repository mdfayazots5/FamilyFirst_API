using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Infrastructure.Services;

/// <summary>
/// Shared rules for objects in the configured bucket (Aws:BucketName / Aws:Region):
/// canonical object URLs, family-prefix validation for client-supplied URLs, and presigned GET read URLs
/// so the bucket can stay private. Used by the vault and task-photo storage services.
/// </summary>
internal static class S3ObjectUrls
{
    private const int ReadExpiryMinutes = 60;

    // AmazonS3Client is thread-safe and meant to be reused; one per region instead of one per call.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AmazonS3Client> Clients = new();

    public static AmazonS3Client Client(string regionName) =>
        Clients.GetOrAdd(regionName, region => new AmazonS3Client(RegionEndpoint.GetBySystemName(region)));

    public static bool TryGetStorage(IConfiguration configuration, out string bucketName, out string regionName)
    {
        bucketName = configuration["Aws:BucketName"] ?? string.Empty;
        regionName = configuration["Aws:Region"] ?? string.Empty;
        return IntegrationSettings.IsConfigured(bucketName) && IntegrationSettings.IsConfigured(regionName);
    }

    public static string ObjectUrl(string bucketName, string regionName, string objectKey) =>
        $"https://{bucketName}.s3.{regionName}.amazonaws.com/{objectKey}";

    /// <summary>
    /// True when <paramref name="url"/> is an object URL under <paramref name="keyPrefix"/> in the configured bucket.
    /// When storage is not configured (local development) any absolute https URL is accepted.
    /// </summary>
    public static bool IsUnderPrefix(IConfiguration configuration, string keyPrefix, string url)
    {
        if (!TryGetStorage(configuration, out var bucketName, out var regionName))
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
        }

        return url.StartsWith(ObjectUrl(bucketName, regionName, keyPrefix), StringComparison.Ordinal)
            && !url.Contains("..", StringComparison.Ordinal)
            && url.IndexOfAny(['?', '#']) < 0;
    }

    /// <summary>Presigned GET for a stored object URL; other URLs (or unconfigured storage) are returned unchanged.</summary>
    public static string CreateReadUrl(IConfiguration configuration, string storedUrl)
    {
        if (!TryGetStorage(configuration, out var bucketName, out var regionName))
        {
            return storedUrl;
        }

        var bucketRoot = ObjectUrl(bucketName, regionName, string.Empty);
        if (!storedUrl.StartsWith(bucketRoot, StringComparison.Ordinal))
        {
            return storedUrl;
        }

        return Client(regionName).GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key        = Uri.UnescapeDataString(storedUrl[bucketRoot.Length..]),
            Verb       = HttpVerb.GET,
            Expires    = DateTime.UtcNow.AddMinutes(ReadExpiryMinutes)
        });
    }
}
