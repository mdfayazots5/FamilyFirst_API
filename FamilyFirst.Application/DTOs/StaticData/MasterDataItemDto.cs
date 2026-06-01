namespace FamilyFirst.Application.DTOs.StaticData;

/// <summary>
/// Single row returned by uspGetMasterDataByCode.
/// Id is the GUID (never the INT PK). Code and Name are the display values.
/// </summary>
public sealed record MasterDataItemDto
{
    public Guid   Id        { get; init; }
    public string Name      { get; init; } = string.Empty;
    public string Code      { get; init; } = string.Empty;
    public int    SortOrder { get; init; }
}
