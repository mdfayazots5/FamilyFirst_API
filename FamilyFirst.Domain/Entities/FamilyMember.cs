using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class FamilyMember : BaseEntity
{
    public long FamilyId { get; set; }

    public long UserId { get; set; }

    public UserRole Role { get; set; }

    public string LinkType { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public long? InvitedByUserId { get; set; }

    public Family? Family { get; set; }

    public User? User { get; set; }

    public User? InvitedByUser { get; set; }
}
