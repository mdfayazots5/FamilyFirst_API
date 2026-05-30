using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class EmergencyCardLink : BaseEntity
{
    public Guid HealthProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Language { get; set; } = "en";

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;
}
