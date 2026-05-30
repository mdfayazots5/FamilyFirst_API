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
        var bucketName = _configuration["Aws:BucketName"];
        var regionName = _configuration["Aws:Region"];

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("AWS bucket configuration is missing.");

        if (string.IsNullOrWhiteSpace(regionName))
            throw new InvalidOperationException("AWS region configuration is missing.");

        var ext = Path.GetExtension(fileName).TrimStart('.');
        var objectKey = $"family/{familyId}/vault/{category.ToString().ToLowerInvariant()}/{Guid.NewGuid()}.{ext}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(UploadExpiryMinutes);

        using var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName));
        var presignRequest = new GetPreSignedUrlRequest
        {
            BucketName  = bucketName,
            Key         = objectKey,
            Verb        = HttpVerb.PUT,
            Expires     = expiresAtUtc,
            ContentType = contentType
        };

        var uploadUrl = s3Client.GetPreSignedURL(presignRequest);
        var fileUrl   = $"https://{bucketName}.s3.{regionName}.amazonaws.com/{objectKey}";

        return Task.FromResult(new VaultUploadUrlDto(uploadUrl, fileUrl, expiresAtUtc));
    }
}
