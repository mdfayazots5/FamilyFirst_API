using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class HeightWeightRecord : BaseEntity
{
    public Guid HealthProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public DateOnly RecordedDate { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public Guid RecordedByUserId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User RecordedByUser { get; set; } = null!;
}
