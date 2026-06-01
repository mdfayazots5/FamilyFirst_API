using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class HeightWeightRecord : BaseEntity
{
    public long HealthProfileId { get; set; }

    public long FamilyId { get; set; }

    public DateTime RecordedDate { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public long RecordedByUserId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User RecordedByUser { get; set; } = null!;
}
