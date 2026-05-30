using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Budget : BaseEntity
{
    public Guid FamilyId { get; set; }

    public string Category { get; set; } = string.Empty;

    public DateOnly MonthYear { get; set; }

    public decimal BudgetAmount { get; set; }

    public Guid SetByUserId { get; set; }

    public Family Family { get; set; } = null!;

    public User SetByUser { get; set; } = null!;
}
