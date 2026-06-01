using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class TeacherProfile : BaseEntity
{
    public long FamilyMemberId { get; set; }

    public long UserId { get; set; }

    public long FamilyId { get; set; }

    public string SubjectName { get; set; } = "General";

    public string TeacherType { get; set; } = "Other";

    public bool IsActive { get; set; } = true;

    public FamilyMember? FamilyMember { get; set; }

    public User? User { get; set; }

    public Family? Family { get; set; }

    public ICollection<TeacherChildAssignment> ChildAssignments { get; } = new List<TeacherChildAssignment>();
}
