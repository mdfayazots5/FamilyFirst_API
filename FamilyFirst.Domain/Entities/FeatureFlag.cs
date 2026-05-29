namespace FamilyFirst.Domain.Entities;

public sealed class FeatureFlag
{
    public string FlagKey { get; set; } = string.Empty;

    public string FlagValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
