using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Attendance;

public sealed class EditAttendanceRequest
{
    public AttendanceStatus Status { get; init; }

    public string? TeacherComment { get; init; }

    public Guid? CommentTemplateId { get; init; }
}
