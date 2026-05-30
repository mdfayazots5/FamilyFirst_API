namespace FamilyFirst.Application.DTOs.Reports;

public sealed record MonthlyReportRequest(
    int? Year,
    int? Month);

public sealed record ExportReportRequest(
    string ReportType,
    string Period,
    Guid? ChildId,
    string Format);
