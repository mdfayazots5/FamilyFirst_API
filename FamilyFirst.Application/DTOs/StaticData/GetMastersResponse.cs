namespace FamilyFirst.Application.DTOs.StaticData;

public sealed record GetMastersResponse
{
    public IReadOnlyCollection<MasterDataItemDto> Items      { get; init; } = Array.Empty<MasterDataItemDto>();
    public int                                    TotalCount { get; init; }
}
