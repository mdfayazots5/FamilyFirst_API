using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public sealed record ReportPdfContent(string Title, string Period, IReadOnlyCollection<ReportPdfSection> Sections);
public sealed record ReportPdfSection(string Title, IReadOnlyCollection<string> Lines);
public sealed record StoredReportPdf(string ObjectKey, string DownloadUrl, DateTime ExpiresAtUtc);

public interface IReportExportStorage
{
    bool IsConfigured { get; }
    Task<StoredReportPdf> StoreAsync(Guid familyId, ReportPdfContent content, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

public interface IReportExportRepository
{
    Task AddAsync(ReportExport export, CancellationToken cancellationToken);
}
