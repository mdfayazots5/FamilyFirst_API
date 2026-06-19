namespace FamilyFirst.Application.DTOs.Reports;

public sealed record ReportArchiveItemDto(
    Guid ArchiveId,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    DateTime GeneratedAt,
    int FamilyScore,
    int ChildCount,
    string? ShareableImageUrl);
