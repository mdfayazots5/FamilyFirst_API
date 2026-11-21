using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FamilyFirst.Application.DTOs.Vault;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Infrastructure.Services;

public sealed class VaultStorageService : IVaultStorageService
{
    private const int UploadExpiryMinutes = 15;

    // The object extension comes from the validated MIME type, never from the client-supplied file name.
    private static readonly IReadOnlyDictionary<string, string> ExtensionByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = "pdf",
            ["image/jpeg"] = "jpg",
            ["image/jpg"] = "jpg",
            ["image/png"] = "png",
            ["image/heic"] = "heic",
            ["image/heif"] = "heif"
        };

    private readonly IConfiguration _configuration;

    public VaultStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<VaultUploadUrlDto> GenerateUploadUrlAsync(
        Guid familyId,
        string fileName,
        string contentType,
        DocumentCategory category,
        CancellationToken cancellationToken)
    {
        if (!S3ObjectUrls.TryGetStorage(_configuration, out var bucketName, out var regionName))
            throw new InvalidOperationException("AWS bucket and region configuration are missing.");
        if (!ExtensionByContentType.TryGetValue(contentType, out var ext))
            throw new InvalidOperationException("Unsupported vault content type.");

        var objectKey = $"family/{familyId}/vault/{category.ToString().ToLowerInvariant()}/{Guid.NewGuid()}.{ext}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(UploadExpiryMinutes);

        var s3Client = S3ObjectUrls.Client(regionName);
        var uploadUrl = s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName  = bucketName,
            Key         = objectKey,
            Verb        = HttpVerb.PUT,
            Expires     = expiresAtUtc,
            ContentType = contentType
        });

        return Task.FromResult(new VaultUploadUrlDto(
            uploadUrl, S3ObjectUrls.ObjectUrl(bucketName, regionName, objectKey), expiresAtUtc));
    }

    public bool IsFamilyObjectUrl(Guid familyId, string fileUrl) =>
        S3ObjectUrls.IsUnderPrefix(_configuration, $"family/{familyId}/vault/", fileUrl);

    public string CreateReadUrl(string storedFileUrl) => S3ObjectUrls.CreateReadUrl(_configuration, storedFileUrl);
}
