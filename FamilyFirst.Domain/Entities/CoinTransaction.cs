namespace FamilyFirst.Domain.Entities;

public sealed class CoinTransaction
{
    public Guid TransactionId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? CreatedByUser { get; set; }
}
