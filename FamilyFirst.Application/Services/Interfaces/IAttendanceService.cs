using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceSessionDto> CreateSessionAsync(Guid currentUserId, Guid familyId, CreateSessionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceSessionDto>> ListSessionsAsync(Guid currentUserId, Guid familyId, DateOnly? scheduledDate, CancellationToken cancellationToken);

    Task<AttendanceSessionDto> GetSessionAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid sessionId, CancellationToken cancellationToken);

    Task<AttendanceSessionDto> SubmitAttendanceAsync(Guid currentUserId, Guid familyId, Guid sessionId, SubmitAttendanceRequest request, CancellationToken cancellationToken);

    Task<AttendanceRecordDto> EditAttendanceRecordAsync(Guid currentUserId, Guid familyId, Guid sessionId, Guid recordId, EditAttendanceRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceRecordDto>> ListChildAttendanceAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid childId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceRecordDto>> ListSessionRecordsAsync(Guid currentUserId, Guid familyId, Guid sessionId, CancellationToken cancellationToken);
}

public interface IAttendanceSessionRepository
{
    Task<AttendanceSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceSession>> ListByTeacherAndDateAsync(Guid teacherProfileId, DateOnly scheduledDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceSession>> ListByAssignedChildrenAndDateAsync(Guid familyId, IReadOnlyCollection<Guid> childProfileIds, DateOnly scheduledDate, CancellationToken cancellationToken);

    Task AddAsync(AttendanceSession session, CancellationToken cancellationToken);

    Task UpdateAsync(AttendanceSession session, CancellationToken cancellationToken);
}

public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceRecord>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttendanceRecord>> ListByChildAndDateRangeAsync(Guid familyId, Guid childProfileId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task AddSubmissionAsync(AttendanceSession session, IReadOnlyCollection<AttendanceRecord> records, CancellationToken cancellationToken);

    Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
}

public interface IPushNotificationService
{
    Task<bool> SendPushAsync(
        string fcmToken,
        string title,
        string body,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? data = null);
}
