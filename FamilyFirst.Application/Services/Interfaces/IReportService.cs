using FamilyFirst.Application.DTOs.Reports;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IReportService
{
    Task<WeeklyDigestDto> GetWeeklyDigestAsync(
        Guid currentUserId,
        Guid familyId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken);

    Task<WeeklyDigestDto> GenerateWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken);

    Task<ChildWeeklyReportDto> GetChildWeeklyReportAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken);

    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);
}
