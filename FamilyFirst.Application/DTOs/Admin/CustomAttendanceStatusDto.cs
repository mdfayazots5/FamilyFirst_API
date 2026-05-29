namespace FamilyFirst.Application.DTOs.Admin;

public sealed record CustomAttendanceStatusDto(
    Guid StatusId,
    Guid? FamilyId,
    string StatusName,
    string ColorHex,
    int SortOrder,
    bool IsDefault,
    DateTime CreatedAt);

public sealed class CreateCustomAttendanceStatusRequest
{
    public string StatusName { get; init; } = string.Empty;

    public string ColorHex { get; init; } = "#64748B";
}
