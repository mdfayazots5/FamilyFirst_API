namespace FamilyFirst.Application.DTOs.Family;

/// <summary>PUT /api/families/{familyId}/members/{memberId}/pin — Parent/FamilyAdmin sets a Child/Elder PIN.</summary>
public sealed class SetMemberPinRequest
{
    public string Pin { get; init; } = string.Empty;
}
