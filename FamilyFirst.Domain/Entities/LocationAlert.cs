using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class LocationAlert : BaseEntity
{
    public long FamilyId { get; set; }

    public long FamilyMemberId { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public long? SafeZoneId { get; set; }

    public string? ZoneNameSnapshot { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public long? ResolvedByUserId { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;

    public SafeZone? SafeZone { get; set; }

    public User? ResolvedByUser { get; set; }
}
