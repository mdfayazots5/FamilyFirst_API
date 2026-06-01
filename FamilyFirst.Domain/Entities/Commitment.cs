using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Commitment : BaseEntity
{
    public long FamilyId { get; set; }

    public long FamilyMemberId { get; set; }

    public string CommitmentName { get; set; } = string.Empty;

    public string CommitmentType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int? DueDay { get; set; }

    public string FrequencyType { get; set; } = "Monthly";

    public DateTime NextDueDate { get; set; }

    public DateTime? LastPaidAt { get; set; }

    public string Status { get; set; } = "Upcoming";

    public bool IsConfirmed { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;
}
