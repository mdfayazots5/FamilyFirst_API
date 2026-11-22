namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminFamilySummaryDto(
    Guid FamilyId,
    string FamilyName,
    string? City,
    bool IsActive,
    int MemberCount,
    DateTime CreatedAt);

public sealed class AdminFamilySearchRequest
{
    public string? Query { get; init; }

    public bool? IsActive { get; init; }

    /// <summary>Optional filter: "Active" (family enabled) or "Flagged" (blocked by SuperAdmin).</summary>
    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
