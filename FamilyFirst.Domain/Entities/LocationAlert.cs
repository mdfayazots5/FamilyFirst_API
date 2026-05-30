using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class LocationAlert : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid FamilyMemberId { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public Guid? ZoneId { get; set; }

    public string? ZoneNameSnapshot { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;

    public SafeZone? Zone { get; set; }

    public User? ResolvedByUser { get; set; }
}
