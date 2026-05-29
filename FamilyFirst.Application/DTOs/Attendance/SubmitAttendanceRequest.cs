using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Attendance;

public sealed class SubmitAttendanceRequest
{
    public IReadOnlyCollection<SubmitAttendanceRecordRequest> Records { get; init; } = Array.Empty<SubmitAttendanceRecordRequest>();
}

public sealed class SubmitAttendanceRecordRequest
{
    public Guid ChildProfileId { get; init; }

    public AttendanceStatus Status { get; init; }

    public string? TeacherComment { get; init; }

    public Guid? CommentTemplateId { get; init; }
}
