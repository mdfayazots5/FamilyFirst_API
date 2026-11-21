using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;

namespace FamilyFirst.Infrastructure.Services;

public sealed class S3ReportExportStorage(IConfiguration configuration) : IReportExportStorage
{
    private string Bucket => configuration["Aws:BucketName"] ?? string.Empty;
    private string Region => configuration["Aws:Region"] ?? string.Empty;

    public bool IsConfigured => IntegrationSettings.IsConfigured(Bucket)
        && IntegrationSettings.IsConfigured(Region)
        && GetLicense() is not null;

    public async Task<StoredReportPdf> StoreAsync(Guid familyId, ReportPdfContent content, CancellationToken cancellationToken)
    {
        var license = GetLicense() ?? throw new InvalidOperationException("A QuestPDF license tier must be configured.");
        cancellationToken.ThrowIfCancellationRequested();
        using var pdf = new MemoryStream(ReportPdfRenderer.Render(content, license), writable: false);
        var client = S3ObjectUrls.Client(Region);
        var objectKey = $"family/{familyId:D}/reports/{Guid.NewGuid():N}.pdf";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS,
            Expires = expiresAt,
        };
        request.ResponseHeaderOverrides.ContentDisposition = "attachment; filename=FamilyFirst-report.pdf";
        var url = client.GetPreSignedURL(request);
        if (url.Length > 4000)
            throw new InvalidOperationException("The report download URL exceeds the supported length.");

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = objectKey,
            InputStream = pdf,
            ContentType = "application/pdf",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        }, cancellationToken);
        return new StoredReportPdf(objectKey, url, expiresAt);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var client = S3ObjectUrls.Client(Region);
        await client.DeleteObjectAsync(Bucket, objectKey, cancellationToken);
    }

    private LicenseType? GetLicense() => configuration["Reports:QuestPdfLicense"]?.Trim().ToUpperInvariant() switch
    {
        "COMMUNITY" => LicenseType.Community,
        "PROFESSIONAL" => LicenseType.Professional,
        "ENTERPRISE" => LicenseType.Enterprise,
        _ => null,
    };
}
