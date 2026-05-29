using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class AttendanceRecord : BaseEntity
{
    public Guid SessionId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? TeacherComment { get; set; }

    public Guid? CommentTemplateId { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

    public Guid MarkedByUserId { get; set; }

    public DateTime? EditedAt { get; set; }

    public Guid? EditedByUserId { get; set; }

    public AttendanceSession? Session { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? MarkedByUser { get; set; }

    public User? EditedByUser { get; set; }
}
