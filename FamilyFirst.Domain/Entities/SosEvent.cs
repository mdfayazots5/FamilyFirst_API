using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class SosEvent : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid LocationAlertId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;

    public int AlertsSentCount { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public Family Family { get; set; } = null!;

    public ChildProfile ChildProfile { get; set; } = null!;

    public LocationAlert LocationAlert { get; set; } = null!;

    public User? ResolvedByUser { get; set; }
}
