using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class FeatureFlag : BaseEntity
{
    public string FlagKey { get; set; } = string.Empty;

    public string FlagValue { get; set; } = string.Empty;

    public string? Description { get; set; }
}
