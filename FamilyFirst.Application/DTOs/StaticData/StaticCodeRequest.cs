namespace FamilyFirst.Application.DTOs.StaticData;

public sealed record StaticCodeRequest
{
    public string ModuleCode { get; init; } = string.Empty;
    public string MethodName { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public int LanguageId { get; init; } = 1;
}
