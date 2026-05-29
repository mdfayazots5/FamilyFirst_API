namespace FamilyFirst.Domain.Entities;

public sealed class ModuleVisibilityConfig
{
    public Guid ConfigId { get; set; } = Guid.NewGuid();

    public Guid? FamilyId { get; set; }

    public int RoleId { get; set; }

    public string ModuleName { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
