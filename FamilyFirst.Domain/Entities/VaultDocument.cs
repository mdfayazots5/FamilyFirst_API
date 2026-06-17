using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultDocument : BaseEntity
{
    public long FamilyId { get; set; }

    public long FamilyMemberId { get; set; }

    public long UploadedByUserId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public DocumentCategory Category { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    public string? Tags { get; set; }

    public bool IsEmergencyPriority { get; set; }

    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.ParentsOnly;

    public int VersionNumber { get; set; } = 1;

    public bool IsCurrentVersion { get; set; } = true;

    public DateTime? PermanentDeleteAt { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;

    public FamilyMember Member
    {
        get => FamilyMember;
        set => FamilyMember = value;
    }

    public User UploadedByUser { get; set; } = null!;

    public ICollection<VaultDocumentVersion> Versions { get; set; } = new List<VaultDocumentVersion>();

    public ICollection<VaultShareLink> ShareLinks { get; set; } = new List<VaultShareLink>();
}
