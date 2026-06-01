namespace FamilyFirst.Application.DTOs.StaticData;

/// <summary>
/// Request DTO for POST /api/GetMasters.
/// MasterDataCode must match a MasterDataCodes enum name exactly (e.g. "TaskType", "ChildProfile").
/// Code is an optional GUID — when provided, returns a single matching record (used to display
/// the current value of a saved dropdown field).
/// </summary>
public sealed record GetMastersRequest
{
    public string  MasterDataCode { get; init; } = string.Empty;
    public string? Code           { get; init; }
    public string? SearchWord     { get; init; }
    public bool    IsPublished    { get; init; } = true;
    public int     PageNumber     { get; init; } = 1;
    public int     PageSize       { get; init; } = 100;
    public int     LanguageId     { get; init; } = 1;
}
