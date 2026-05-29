namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminFamilySummaryDto(
    Guid FamilyId,
    string FamilyName,
    string? City,
    string PlanCode,
    string PlanName,
    string SubscriptionStatus,
    bool IsActive,
    int MemberCount,
    DateTime CreatedAt);

public sealed class AdminFamilySearchRequest
{
    public string? Query { get; init; }

    public string? PlanCode { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
