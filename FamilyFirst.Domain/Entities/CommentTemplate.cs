namespace FamilyFirst.Domain.Entities;

public sealed class CommentTemplate
{
    public Guid TemplateId { get; set; }

    public Guid? FamilyId { get; set; }

    public string TemplateText { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
