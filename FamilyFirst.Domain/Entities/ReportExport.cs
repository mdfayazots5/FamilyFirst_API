using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class ReportExport : BaseEntity
{
    public long FamilyId { get; set; }
    public long RequestedByUserId { get; set; }
    public long? ChildProfileId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public string? DownloadUrl { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
