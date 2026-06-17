using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class AttendanceRecord : BaseEntity
{
    public long AttendanceSessionId { get; set; }

    public Guid SessionId => AttendanceSession?.Id ?? Guid.Empty;

    public long ChildProfileId { get; set; }

    public long FamilyId { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? TeacherComment { get; set; }

    public long? CommentTemplateId { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

    public long MarkedByUserId { get; set; }

    public DateTime? EditedAt { get; set; }

    public long? EditedByUserId { get; set; }

    public AttendanceSession? AttendanceSession { get; set; }

    public AttendanceSession? Session
    {
        get => AttendanceSession;
        set => AttendanceSession = value;
    }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? MarkedByUser { get; set; }

    public User? EditedByUser { get; set; }
}
