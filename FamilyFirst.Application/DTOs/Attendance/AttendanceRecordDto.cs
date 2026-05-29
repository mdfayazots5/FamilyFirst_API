using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Attendance;

public sealed record AttendanceRecordDto(
    Guid RecordId,
    Guid SessionId,
    Guid ChildProfileId,
    Guid FamilyId,
    string ChildName,
    AttendanceStatus Status,
    string? TeacherComment,
    Guid? CommentTemplateId,
    DateTime MarkedAt,
    Guid MarkedByUserId,
    DateTime? EditedAt,
    Guid? EditedByUserId);
