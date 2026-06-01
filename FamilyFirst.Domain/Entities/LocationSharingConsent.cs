using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class LocationSharingConsent : BaseEntity
{
    public long FamilyId { get; set; }

    public long FamilyMemberId { get; set; }

    public bool ConsentGiven { get; set; }

    public bool SharingEnabled { get; set; }

    public bool CaregiverViewOnly { get; set; }

    public DateTime? ConsentGivenAt { get; set; }

    public DateTime? ConsentRevokedAt { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;
}
