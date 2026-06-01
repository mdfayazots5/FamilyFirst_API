using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class WeeklyDigestArchive : BaseEntity
{
    public long FamilyId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public string DigestContentJson { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public string? ShareableImageUrl { get; set; }

    public Family Family { get; set; } = null!;
}
