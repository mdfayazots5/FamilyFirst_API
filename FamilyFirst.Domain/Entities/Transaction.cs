using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Transaction : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid FamilyMemberId { get; set; }

    public string? MerchantName { get; set; }

    public string? MerchantNameHash { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = "Debit";

    public string Category { get; set; } = string.Empty;

    public int PrivacyTierAtCapture { get; set; }

    public bool IsCommitment { get; set; }

    public Guid? CommitmentId { get; set; }

    public string QuestionStatus { get; set; } = "None";

    public string? RawSmsText { get; set; }

    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;

    public Commitment? Commitment { get; set; }
}
