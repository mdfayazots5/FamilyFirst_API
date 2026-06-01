namespace FamilyFirst.Application.DTOs.StaticData;

public sealed record StaticSearchRequest
{
    public string ModuleCode { get; init; } = string.Empty;
    public string MethodName { get; init; } = string.Empty;
    public string? SearchWord { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int LanguageId { get; init; } = 1;
}
