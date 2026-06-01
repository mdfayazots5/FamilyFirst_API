using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultShareLink : BaseEntity
{
    public long VaultDocumentId { get; set; }

    public long FamilyId { get; set; }

    public long CreatedByUserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool AllowDownload { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public VaultDocument VaultDocument { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;
}
