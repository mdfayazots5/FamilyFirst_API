using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class SosEvent : BaseEntity
{
    public long FamilyId { get; set; }

    public long ChildProfileId { get; set; }

    public long LocationAlertId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;

    public int AlertsSentCount { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public long? ResolvedByUserId { get; set; }

    public Family Family { get; set; } = null!;

    public ChildProfile ChildProfile { get; set; } = null!;

    public LocationAlert LocationAlert { get; set; } = null!;

    public User? ResolvedByUser { get; set; }
}
