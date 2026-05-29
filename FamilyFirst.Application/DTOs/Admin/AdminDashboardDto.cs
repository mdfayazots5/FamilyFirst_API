namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminDashboardDto(
    int TotalFamilies,
    int ActiveFamilies,
    decimal RevenueMonthly,
    int ChurnCount,
    int SignupsToday);
