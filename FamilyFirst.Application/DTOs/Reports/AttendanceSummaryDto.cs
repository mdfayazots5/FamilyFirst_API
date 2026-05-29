namespace FamilyFirst.Application.DTOs.Reports;

public sealed record AttendanceSummaryDto(
    Guid ChildProfileId,
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalSessions,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int LeftEarlyCount,
    decimal AttendanceRate,
    IReadOnlyCollection<AttendanceHeatmapEntryDto> Heatmap);

public sealed record AttendanceHeatmapEntryDto(
    DateOnly Date,
    string Status,
    int SessionCount);
