namespace FamilyFirst.Domain.Entities;

public sealed class AuditLog
{
    public long AuditId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? FamilyId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Family? Family { get; set; }
}
