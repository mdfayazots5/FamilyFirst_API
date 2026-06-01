namespace FamilyFirst.Application.DTOs.StaticData;

public sealed record StaticDataResponse
{
    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Items { get; init; }
        = Array.Empty<IReadOnlyDictionary<string, object?>>();

    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}
