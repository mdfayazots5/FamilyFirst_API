using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultDocumentVersion : BaseEntity
{
    public long VaultDocumentId { get; set; }

    public long FamilyId { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public long UploadedByUserId { get; set; }

    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    public VaultDocument VaultDocument { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User UploadedByUser { get; set; } = null!;
}
