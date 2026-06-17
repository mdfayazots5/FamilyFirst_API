using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class CustomAttendanceStatus : BaseEntity
{
    public Guid StatusId => Id;

    public long FamilyId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public string ColorHex { get; set; } = "#64748B";

    public Family? Family { get; set; }
}
