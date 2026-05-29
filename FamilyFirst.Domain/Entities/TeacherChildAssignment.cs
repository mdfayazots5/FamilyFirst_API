using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class TeacherChildAssignment : BaseEntity
{
    public Guid TeacherProfileId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public TeacherProfile? TeacherProfile { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }
}
