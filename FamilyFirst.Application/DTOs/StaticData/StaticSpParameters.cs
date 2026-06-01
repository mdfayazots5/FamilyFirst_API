namespace FamilyFirst.Application.DTOs.StaticData;

public sealed record StaticSpParameters
{
    public long FamilyId { get; init; }
    public long UserId { get; init; }
    public int RoleId { get; init; }
    public string? Id { get; init; }
    public string? SearchWord { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int LanguageId { get; init; } = 1;
}
