using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class FinanceConsent : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid FamilyMemberId { get; set; }

    public int PrivacyTier { get; set; } = 2;

    public string ConsentStatus { get; set; } = "NotInvited";

    public string? ConsentToken { get; set; }

    public DateTime? InvitedAt { get; set; }

    public DateTime? ConsentGivenAt { get; set; }

    public string? ConsentVersion { get; set; }

    public string? ConsentIpAddress { get; set; }

    public DateTime? OptedOutAt { get; set; }

    public DateTime? LastReminderSentAt { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;
}
