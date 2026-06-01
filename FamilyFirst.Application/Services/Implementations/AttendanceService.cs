using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class AttendanceService : IAttendanceService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAttendanceRecordRepository _attendanceRecordRepository;
    private readonly IAttendanceSessionRepository _attendanceSessionRepository;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITeacherChildAssignmentRepository _teacherChildAssignmentRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;

    public AttendanceService(
        IAttendanceSessionRepository attendanceSessionRepository,
        IAttendanceRecordRepository attendanceRecordRepository,
        IAuditLogRepository auditLogRepository,
        IFamilyMemberRepository familyMemberRepository,
        ITeacherProfileRepository teacherProfileRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IPushNotificationService pushNotificationService)
    {
        _attendanceSessionRepository = attendanceSessionRepository;
        _attendanceRecordRepository = attendanceRecordRepository;
        _auditLogRepository = auditLogRepository;
        _familyMemberRepository = familyMemberRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _childProfileRepository = childProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<AttendanceSessionDto> CreateSessionAsync(
        Guid currentUserId,
        Guid familyId,
        CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Teacher and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Teacher or FamilyAdmin role is required to create attendance sessions.");
        }

        var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);
        var session = new AttendanceSession
        {
            TeacherProfileId = teacherProfile.InternalId,
            FamilyId = member.FamilyId,
            SessionName = request.SessionName.Trim(),
            SubjectName = request.SubjectName.Trim(),
            BatchName = string.IsNullOrWhiteSpace(request.BatchName) ? null : request.BatchName.Trim(),
            ScheduledDate = request.ScheduledDate!.Value.ToDateTime(TimeOnly.MinValue),
            StartTime = new DateTime(1900, 1, 1).Add(request.StartTime!.Value.ToTimeSpan()),
            EndTime = request.EndTime.HasValue ? new DateTime(1900, 1, 1).Add(request.EndTime.Value.ToTimeSpan()) : (DateTime?)null,
            IsRecurring = request.IsRecurring,
            RecurringDays = CreateRecurringDaysJson(request),
            IsActive = true
        };

        await _attendanceSessionRepository.AddAsync(session, cancellationToken);
        session.TeacherProfile = teacherProfile;

        return ToDto(session);
    }

    public async Task<IReadOnlyCollection<AttendanceSessionDto>> ListSessionsAsync(
        Guid currentUserId,
        Guid familyId,
        DateOnly? scheduledDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var sessionDate = scheduledDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (member.Role == UserRole.Teacher)
        {
            var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);
            var teacherSessions = await _attendanceSessionRepository.ListByTeacherAndDateAsync(
                teacherProfile.Id,
                sessionDate,
                cancellationToken);

            return teacherSessions.Select(ToDto).ToArray();
        }

        if (member.Role is UserRole.Parent or UserRole.FamilyAdmin)
        {
            var childProfileIds = (await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken))
                .Select(child => child.Id)
                .ToArray();
            var parentSessions = await _attendanceSessionRepository.ListByAssignedChildrenAndDateAsync(
                familyId,
                childProfileIds,
                sessionDate,
                cancellationToken);

            return parentSessions.Select(ToDto).ToArray();
        }

        throw new ForbiddenAccessException("Teacher or Parent role is required to list attendance sessions.");
    }

    public async Task<AttendanceSessionDto> GetSessionAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionInFamilyOrThrowAsync(sessionId, familyId, cancellationToken);
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role == UserRole.FamilyAdmin)
        {
            return ToDto(session);
        }

        if (member.Role == UserRole.Teacher && await IsTeacherSessionAsync(member, familyId, session, cancellationToken))
        {
            return ToDto(session);
        }

        if (member.Role == UserRole.Parent && await IsVisibleToFamilyChildrenAsync(session, familyId, cancellationToken))
        {
            return ToDto(session);
        }

        if (member.Role == UserRole.Child
            && currentChildProfileId.HasValue
            && await IsTeacherAssignedToChildAsync(session.TeacherProfile?.Id ?? Guid.Empty, currentChildProfileId.Value, cancellationToken))
        {
            return ToDto(session);
        }

        throw new ForbiddenAccessException("Attendance session access is not allowed.");
    }

    public async Task<AttendanceSessionDto> SubmitAttendanceAsync(
        Guid currentUserId,
        Guid familyId,
        Guid sessionId,
        SubmitAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionInFamilyOrThrowAsync(sessionId, familyId, cancellationToken);

        if (session.IsSubmitted)
        {
            throw new ConflictException("Attendance session has already been submitted.");
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Teacher)
        {
            throw new ForbiddenAccessException("Teacher role is required to submit attendance.");
        }

        var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);

        if (session.TeacherProfileId != teacherProfile.InternalId)
        {
            throw new ForbiddenAccessException("Teacher can submit only their own attendance sessions.");
        }

        var assignedChildIds = await _teacherChildAssignmentRepository.ListActiveChildIdsByTeacherProfileIdAsync(
            teacherProfile.Id,
            cancellationToken);

        if (assignedChildIds.Count == 0)
        {
            throw new ConflictException("Teacher has no assigned children for attendance submission.");
        }

        EnsureRequestedChildrenAreAssigned(request, assignedChildIds);

        var childProfiles = (await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken))
            .Where(child => assignedChildIds.Contains(child.Id))
            .ToDictionary(child => child.Id);
        var attendanceRecords = CreateSubmissionRecords(request, assignedChildIds, childProfiles, member.FamilyId, session.InternalId, member.UserId);
        var utcNow = DateTime.UtcNow;

        session.IsSubmitted = true;
        session.SubmittedAt = utcNow;

        await _attendanceRecordRepository.AddSubmissionAsync(session, attendanceRecords, cancellationToken);
        await SendAttendanceAlertNotificationsAsync(session, attendanceRecords, childProfiles, cancellationToken);

        return ToDto(session);
    }

    public async Task<AttendanceRecordDto> EditAttendanceRecordAsync(
        Guid currentUserId,
        Guid familyId,
        Guid sessionId,
        Guid recordId,
        EditAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var record = await GetRecordInSessionOrThrowAsync(recordId, sessionId, familyId, cancellationToken);
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        await EnsureCanEditRecordAsync(member, record.AttendanceSession!, cancellationToken);

        var oldValues = CreateAuditValues(record);
        var oldStatus = record.Status;
        record.Status = request.Status;
        record.TeacherComment = NormalizeComment(request.TeacherComment);
        record.CommentTemplateId = null; // CommentTemplateId is long? in entity; Guid? from DTO — skip lookup for now
        record.EditedAt = DateTime.UtcNow;
        record.EditedByUserId = member.UserId;
        var newValues = CreateAuditValues(record);

        await _attendanceRecordRepository.UpdateAsync(record, cancellationToken);

        if (member.Role == UserRole.FamilyAdmin)
        {
            await _auditLogRepository.AddAsync(
                new AuditLog
                {
                    UserId = member.UserId,
                    FamilyId = member.FamilyId,
                    Action = "AttendanceEdited",
                    EntityType = nameof(AttendanceRecord),
                    EntityId = record.Id.ToString(),
                    OldValues = JsonSerializer.Serialize(oldValues, AuditJsonOptions),
                    NewValues = JsonSerializer.Serialize(newValues, AuditJsonOptions)
                },
                cancellationToken);
        }

        if (oldStatus != record.Status && IsAttendanceAlertStatus(record.Status))
        {
            await SendAttendanceAlertNotificationsAsync(
                record.AttendanceSession!,
                new[] { record },
                new Dictionary<Guid, ChildProfile> { [record.ChildProfile!.Id] = record.ChildProfile! },
                cancellationToken);
        }

        return ToRecordDto(record);
    }

    public async Task<IReadOnlyCollection<AttendanceRecordDto>> ListChildAttendanceAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role == UserRole.Child && currentChildProfileId != child.Id)
        {
            throw new ForbiddenAccessException("Child can view only their own attendance history.");
        }

        if (member.Role is not UserRole.Parent and not UserRole.Child)
        {
            throw new ForbiddenAccessException("Parent or Child role is required to view attendance history.");
        }

        var records = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
            familyId,
            child.Id,
            fromDate,
            toDate,
            cancellationToken);

        return records.Select(ToRecordDto).ToArray();
    }

    public async Task<IReadOnlyCollection<AttendanceRecordDto>> ListSessionRecordsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionInFamilyOrThrowAsync(sessionId, familyId, cancellationToken);
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role == UserRole.Teacher && !await IsTeacherSessionAsync(member, familyId, session, cancellationToken))
        {
            throw new ForbiddenAccessException("Teacher can view only their own attendance session records.");
        }

        if (member.Role is not UserRole.Teacher and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Teacher or FamilyAdmin role is required to view attendance session records.");
        }

        var records = await _attendanceRecordRepository.ListBySessionAsync(session.Id, cancellationToken);

        return records.Select(ToRecordDto).ToArray();
    }

    private async Task<AttendanceSession> GetSessionInFamilyOrThrowAsync(Guid sessionId, Guid familyId, CancellationToken cancellationToken)
    {
        var session = await _attendanceSessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session is null || session.Family?.Id != familyId || !session.IsActive)
        {
            throw new NotFoundException(nameof(AttendanceSession), sessionId);
        }

        return session;
    }

    private async Task<AttendanceRecord> GetRecordInSessionOrThrowAsync(
        Guid recordId,
        Guid sessionId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceRecordRepository.GetByIdAsync(recordId, cancellationToken);

        if (record is null || record.AttendanceSession?.Id != sessionId || record.AttendanceSession?.Family?.Id != familyId || record.AttendanceSession is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), recordId);
        }

        return record;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childId, Guid familyId, CancellationToken cancellationToken)
    {
        var child = await _childProfileRepository.GetByIdAsync(childId, cancellationToken);

        if (child is null || child.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childId);
        }

        return child;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task<TeacherProfile> GetTeacherProfileForMemberAsync(
        FamilyMember member,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var teacherProfile = await _teacherProfileRepository.GetByFamilyMemberIdAsync(member.Id, cancellationToken);

        if (teacherProfile is null || teacherProfile.Family?.Id != familyId || !teacherProfile.IsActive)
        {
            throw new ForbiddenAccessException("An active teacher profile in this family is required.");
        }

        return teacherProfile;
    }

    private async Task<bool> IsTeacherSessionAsync(
        FamilyMember member,
        Guid familyId,
        AttendanceSession session,
        CancellationToken cancellationToken)
    {
        var teacherProfile = await _teacherProfileRepository.GetByFamilyMemberIdAsync(member.Id, cancellationToken);

        return teacherProfile is not null
            && teacherProfile.Family?.Id == familyId
            && teacherProfile.IsActive
            && teacherProfile.InternalId == session.TeacherProfileId;
    }

    private async Task<bool> IsVisibleToFamilyChildrenAsync(
        AttendanceSession session,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var childProfileIds = (await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken))
            .Select(child => child.Id)
            .ToArray();

        if (childProfileIds.Length == 0)
        {
            return false;
        }

        var assignedChildIds = await _teacherChildAssignmentRepository.ListActiveChildIdsByTeacherProfileIdAsync(
            session.TeacherProfile?.Id ?? Guid.Empty,
            cancellationToken);

        return assignedChildIds.Intersect(childProfileIds).Any();
    }

    private async Task<bool> IsTeacherAssignedToChildAsync(
        Guid teacherProfileId,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        return await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
            teacherProfileId,
            childProfileId,
            cancellationToken) is not null;
    }

    private async Task EnsureCanEditRecordAsync(
        FamilyMember member,
        AttendanceSession session,
        CancellationToken cancellationToken)
    {
        if (member.Role == UserRole.FamilyAdmin)
        {
            return;
        }

        if (member.Role != UserRole.Teacher || !await IsTeacherSessionAsync(member, session.Family?.Id ?? Guid.Empty, session, cancellationToken))
        {
            throw new ForbiddenAccessException("Teacher or FamilyAdmin role is required to edit attendance records.");
        }

        if (!session.SubmittedAt.HasValue || DateTime.UtcNow - session.SubmittedAt.Value >= TimeSpan.FromHours(1))
        {
            throw new ForbiddenAccessException("Teacher attendance edit window has expired.");
        }
    }

    private static IReadOnlyCollection<AttendanceRecord> CreateSubmissionRecords(
        SubmitAttendanceRequest request,
        IReadOnlyCollection<Guid> assignedChildIds,
        IReadOnlyDictionary<Guid, ChildProfile> childProfiles,
        long familyId,
        long sessionId,
        long markedByUserId)
    {
        var requestedRecordsByChildId = request.Records
            .GroupBy(record => record.ChildProfileId)
            .ToDictionary(group => group.Key, group => group.First());
        var utcNow = DateTime.UtcNow;

        return assignedChildIds
            .Select(childProfileGuid =>
            {
                requestedRecordsByChildId.TryGetValue(childProfileGuid, out var requestedRecord);
                var childInternalId = childProfiles.TryGetValue(childProfileGuid, out var cp) ? cp.InternalId : 0L;

                return new AttendanceRecord
                {
                    AttendanceSessionId = sessionId,
                    ChildProfileId = childInternalId,
                    FamilyId = familyId,
                    Status = requestedRecord?.Status ?? AttendanceStatus.Present,
                    TeacherComment = NormalizeComment(requestedRecord?.TeacherComment),
                    CommentTemplateId = null, // CommentTemplateId is long? in entity; Guid? from DTO — skip lookup
                    MarkedAt = utcNow,
                    MarkedByUserId = markedByUserId
                };
            })
            .ToArray();
    }

    private static void EnsureRequestedChildrenAreAssigned(
        SubmitAttendanceRequest request,
        IReadOnlyCollection<Guid> assignedChildIds)
    {
        var unassignedChildIds = request.Records
            .Select(record => record.ChildProfileId)
            .Where(childProfileId => !assignedChildIds.Contains(childProfileId))
            .ToArray();

        if (unassignedChildIds.Length > 0)
        {
            throw new ForbiddenAccessException("All submitted children must be actively assigned to the teacher.");
        }
    }

    private async Task SendAttendanceAlertNotificationsAsync(
        AttendanceSession session,
        IReadOnlyCollection<AttendanceRecord> records,
        IReadOnlyDictionary<Guid, ChildProfile> childProfiles,
        CancellationToken cancellationToken)
    {
        var alertRecords = records
            .Where(record => IsAttendanceAlertStatus(record.Status))
            .ToArray();

        if (alertRecords.Length == 0)
        {
            return;
        }

        var parentFcmTokens = (await _familyMemberRepository.ListActiveByFamilyAsync(session.Family?.Id ?? Guid.Empty, cancellationToken))
            .Where(member => member.Role is UserRole.Parent or UserRole.FamilyAdmin)
            .Select(member => member.User?.FcmToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => token!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var record in alertRecords)
        {
            var childProfile = childProfiles.TryGetValue(record.ChildProfileId, out var mappedChild)
                ? mappedChild
                : record.ChildProfile;
            var childName = childProfile?.FamilyMember?.User?.FullName ?? childProfile?.User?.FullName ?? "Child";
            var body = $"{childName} was marked {record.Status} today by {GetTeacherName(session)} - {session.SubjectName}";

            foreach (var fcmToken in parentFcmTokens)
            {
                await _pushNotificationService.SendPushAsync(
                    fcmToken,
                    "Attendance update",
                    body,
                    cancellationToken);
            }
        }
    }

    private static bool IsAttendanceAlertStatus(AttendanceStatus status)
    {
        return status is AttendanceStatus.Absent or AttendanceStatus.Late;
    }

    private static AttendanceRecordDto ToRecordDto(AttendanceRecord record)
    {
        return new AttendanceRecordDto(
            record.Id,
            record.AttendanceSession?.Id ?? Guid.Empty,
            record.ChildProfile?.Id ?? Guid.Empty,
            record.AttendanceSession?.Family?.Id ?? Guid.Empty,
            record.ChildProfile?.FamilyMember?.User?.FullName ?? record.ChildProfile?.User?.FullName ?? string.Empty,
            record.Status,
            record.TeacherComment,
            null, // CommentTemplateId: long? in entity, Guid? in DTO — not mappable directly
            record.MarkedAt,
            record.MarkedByUser?.Id ?? Guid.Empty,
            record.EditedAt,
            record.EditedByUser?.Id);
    }

    private static AttendanceRecordAuditValues CreateAuditValues(AttendanceRecord record)
    {
        return new AttendanceRecordAuditValues(
            record.Status,
            record.TeacherComment,
            null); // CommentTemplateId: long? in entity, Guid? in audit record — skip
    }

    private static string? NormalizeComment(string? teacherComment)
    {
        return string.IsNullOrWhiteSpace(teacherComment) ? null : teacherComment.Trim();
    }

    private static string GetTeacherName(AttendanceSession session)
    {
        return session.TeacherProfile?.FamilyMember?.User?.FullName
            ?? session.TeacherProfile?.User?.FullName
            ?? "Teacher";
    }

    private static string? CreateRecurringDaysJson(CreateSessionRequest request)
    {
        if (!request.IsRecurring)
        {
            return null;
        }

        var recurringDays = request.RecurringDays!
            .OrderBy(day => day)
            .ToArray();

        return JsonSerializer.Serialize(recurringDays);
    }

    private static AttendanceSessionDto ToDto(AttendanceSession session)
    {
        return new AttendanceSessionDto(
            session.Id,
            session.TeacherProfile?.Id ?? Guid.Empty,
            session.Family?.Id ?? Guid.Empty,
            session.TeacherProfile?.FamilyMember?.User?.FullName ?? session.TeacherProfile?.User?.FullName ?? string.Empty,
            session.SessionName,
            session.SubjectName,
            session.BatchName,
            DateOnly.FromDateTime(session.ScheduledDate),
            TimeOnly.FromDateTime(session.StartTime),
            session.EndTime.HasValue ? TimeOnly.FromDateTime(session.EndTime.Value) : (TimeOnly?)null,
            session.IsSubmitted,
            session.SubmittedAt,
            session.IsRecurring,
            ParseRecurringDays(session.RecurringDays),
            session.IsActive);
    }

    private static IReadOnlyCollection<int> ParseRecurringDays(string? recurringDays)
    {
        if (string.IsNullOrWhiteSpace(recurringDays))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<int[]>(recurringDays) ?? Array.Empty<int>();
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }

    private sealed record AttendanceRecordAuditValues(
        AttendanceStatus Status,
        string? TeacherComment,
        Guid? CommentTemplateId);
}
