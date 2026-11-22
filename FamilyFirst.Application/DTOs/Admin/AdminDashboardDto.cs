namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminDashboardDto(
    int TotalFamilies,
    int ActiveFamilies,
    int SignupsToday);
