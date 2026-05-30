using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class FinanceSetting : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid? CfoFamilyMemberId { get; set; }

    public bool IsModuleEnabled { get; set; }

    public DateTime? EnabledAt { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember? CfoFamilyMember { get; set; }
}
