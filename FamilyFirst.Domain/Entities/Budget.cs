using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Budget : BaseEntity
{
    public long FamilyId { get; set; }

    public string Category { get; set; } = string.Empty;

    public DateTime MonthYear { get; set; }

    public decimal BudgetAmount { get; set; }

    public long SetByUserId { get; set; }

    public Family Family { get; set; } = null!;

    public User SetByUser { get; set; } = null!;
}
