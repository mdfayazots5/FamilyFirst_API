namespace FamilyFirst.Domain.Entities;

public sealed class CustomAttendanceStatus
{
    public Guid StatusId { get; set; } = Guid.NewGuid();

    public Guid FamilyId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public string ColorHex { get; set; } = "#64748B";

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
