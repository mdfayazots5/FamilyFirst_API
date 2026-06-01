using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class ModuleVisibilityConfig : BaseEntity
{
    public long? FamilyId { get; set; }

    public int RoleId { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public Family? Family { get; set; }
}
