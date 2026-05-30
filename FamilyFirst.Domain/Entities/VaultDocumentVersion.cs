using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultDocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Guid FamilyId { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    public VaultDocument Document { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public User UploadedByUser { get; set; } = null!;
}
