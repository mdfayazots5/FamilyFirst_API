using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Infrastructure.Services;

public sealed class S3StorageService : IS3StorageService
{
    private const int UploadExpiryMinutes = 15;

    private readonly IConfiguration _configuration;

    public S3StorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<TaskCompletionUploadUrlDto> GenerateTaskCompletionUploadUrlAsync(
        Guid familyId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var bucketName = _configuration["Aws:BucketName"];
        var regionName = _configuration["Aws:Region"];

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("AWS bucket configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException("AWS region configuration is missing.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(UploadExpiryMinutes);
        var objectKey = $"family/{familyId}/tasks/{taskId}/{Guid.NewGuid()}.jpg";

        var s3Client = S3ObjectUrls.Client(regionName);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAtUtc,
            ContentType = "image/jpeg"
        };

        var uploadUrl = s3Client.GetPreSignedURL(request);

        return Task.FromResult(new TaskCompletionUploadUrlDto(taskId, uploadUrl, objectKey, expiresAtUtc));
    }

    public bool IsTaskPhotoUrl(Guid familyId, Guid taskId, string photoUrl) =>
        S3ObjectUrls.IsUnderPrefix(_configuration, $"family/{familyId}/tasks/{taskId}/", photoUrl);

    public string CreateReadUrl(string storedPhotoUrl) => S3ObjectUrls.CreateReadUrl(_configuration, storedPhotoUrl);
}
