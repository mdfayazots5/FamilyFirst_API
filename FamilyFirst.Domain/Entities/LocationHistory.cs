using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

// Append-only — hard-deleted after 30 days by SafetyWorker (DPDP Act 2023).
public sealed class LocationHistory : AppendOnlyEntity
{
    public long FamilyId { get; set; }

    public long FamilyMemberId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public int BatteryLevel { get; set; }

    public string? LocationName { get; set; }

    public DateTime RecordedAt { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;
}
