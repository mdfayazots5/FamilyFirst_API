using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

// Append-only ledger — no IsDeleted, no updates. Every coin mutation writes one row.
public sealed class CoinTransaction : AppendOnlyEntity
{
    public long ChildProfileId { get; set; }

    public long FamilyId { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    // Soft reference to triggering entity's BIGINT PK — no FK constraint (polymorphic)
    public long? ReferenceId { get; set; }

    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? CreatedByUser { get; set; }
}
