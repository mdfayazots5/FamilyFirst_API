namespace FamilyFirst.Domain.Entities;

// NOT a BaseEntity — append-only table, hard-deleted after 30 days by SafetyWorker (DPDP Act 2023).
public sealed class LocationHistory
{
    public Guid LocationHistoryId { get; set; } = Guid.NewGuid();

    public Guid FamilyId { get; set; }

    public Guid FamilyMemberId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public int BatteryLevel { get; set; }

    public string? LocationName { get; set; }

    public DateTime RecordedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;
}
