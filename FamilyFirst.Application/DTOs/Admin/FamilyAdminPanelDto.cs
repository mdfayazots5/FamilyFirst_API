using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed record FamilyAdminPanelDto(
    Guid FamilyId,
    string FamilyName,
    IReadOnlyCollection<FamilyAdminPanelMemberDto> Members,
    FamilyAdminPanelStatsDto Stats);

public sealed record FamilyAdminPanelMemberDto(
    Guid FamilyMemberId,
    Guid UserId,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime JoinedAt,
    int AttendanceCountThisWeek,
    int TaskCompletionsThisWeek,
    int FeedbackCountThisWeek);

public sealed record FamilyAdminPanelStatsDto(
    int TotalMembers,
    int ParentsCount,
    int ChildrenCount,
    int TeachersCount,
    int EldersCount,
    int AttendanceRecordsThisWeek,
    int TaskCompletionsThisWeek,
    int FeedbackEntriesThisWeek);
