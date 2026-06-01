using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

// Append-only audit log — no IsDeleted, no UpdatedBy. Hard-retained for compliance.
public sealed class AuditLog : AppendOnlyEntity
{
    public long? UserId { get; set; }

    public long? FamilyId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? UserAgent { get; set; }

    public User? User { get; set; }

    public Family? Family { get; set; }
}
