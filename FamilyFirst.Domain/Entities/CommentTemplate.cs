using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class CommentTemplate : BaseEntity
{
    public Guid TemplateId => Id;

    public long? FamilyId { get; set; }

    public string TemplateText { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public Family? Family { get; set; }
}
