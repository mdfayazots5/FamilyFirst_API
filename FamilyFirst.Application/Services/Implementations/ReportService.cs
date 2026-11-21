using System.Globalization;
using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Reports;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class ReportService : IReportService
{
    private readonly IAttendanceRecordRepository _attendanceRecordRepository;
    private readonly ICalendarEventRepository _calendarEventRepository;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IFamilyRepository _familyRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ITaskCompletionRepository _taskCompletionRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IWeeklyDigestArchiveRepository _archiveRepository;
    private readonly IErrorCodeService _errorCodeService;
    // Level 2 dependencies — all registered in DependencyInjection.cs (required since R5)
    private readonly IVaultDocumentRepository _vaultDocumentRepository;
    private readonly IMedicalRepository        _medicalRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;

    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _exportPermissionService;
    private readonly IReportExportRepository _reportExports;
    private readonly IReportExportStorage _reportExportStorage;
    private readonly IFinanceService _financeService;

    public ReportService(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        IAttendanceRecordRepository attendanceRecordRepository,
        ITaskItemRepository taskItemRepository,
        ITaskCompletionRepository taskCompletionRepository,
        IFeedbackRepository feedbackRepository,
        ICalendarEventRepository calendarEventRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IWeeklyDigestArchiveRepository weeklyDigestArchiveRepository,
        IErrorCodeService errorCodeService,
        IApiLogService apiLogService,
        IVaultDocumentRepository vaultDocumentRepository,
        IMedicalRepository medicalRepository,
        IPermissionService exportPermissionService,
        IReportExportRepository reportExports,
        IReportExportStorage reportExportStorage,
        IFinanceService financeService)
    {
        _apiLogService = apiLogService;
        _errorCodeService = errorCodeService;
        _familyRepository               = familyRepository;
        _familyMemberRepository         = familyMemberRepository;
        _childProfileRepository         = childProfileRepository;
        _attendanceRecordRepository     = attendanceRecordRepository;
        _taskItemRepository             = taskItemRepository;
        _taskCompletionRepository       = taskCompletionRepository;
        _feedbackRepository             = feedbackRepository;
        _calendarEventRepository        = calendarEventRepository;
        _coinTransactionRepository      = coinTransactionRepository;
        _archiveRepository              = weeklyDigestArchiveRepository;
        _vaultDocumentRepository        = vaultDocumentRepository;
        _medicalRepository              = medicalRepository;
        _exportPermissionService = exportPermissionService;
        _reportExports = reportExports;
        _reportExportStorage = reportExportStorage;
        _financeService = financeService;
    }

    public async Task<WeeklyDigestDto> GetWeeklyDigestAsync(
        Guid currentUserId,
        Guid familyId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!await IsReportReaderAsync(member, cancellationToken))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return LogResult(nameof(GetWeeklyDigestAsync), familyId, await BuildWeeklyDigestAsync(
            familyId,
            await ResolveWeekStartDateAsync(weekStartDate, cancellationToken),
            cancellationToken));
    }

    public async Task<WeeklyDigestDto> GenerateWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        return LogResult(nameof(GenerateWeeklyDigestAsync), familyId, await BuildWeeklyDigestAsync(familyId, await ResolveWeekStartDateAsync(weekStartDate, cancellationToken), cancellationToken));
    }

    public async Task<ChildWeeklyReportDto> GetChildWeeklyReportAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!await IsReportReaderAsync(member, cancellationToken))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var resolvedWeekStartDate = await ResolveWeekStartDateAsync(weekStartDate, cancellationToken);
        var weekEndDate = resolvedWeekStartDate.AddDays(6);
        var attendanceRecords = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
            familyId,
            childId,
            resolvedWeekStartDate,
            weekEndDate,
            cancellationToken);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var taskCompletions = (await _taskCompletionRepository.ListByFamilyAsync(
                familyId,
                childId,
                null,
                cancellationToken))
            .Where(taskCompletion =>
            {
                var scheduledDate = DateOnly.FromDateTime(taskCompletion.ScheduledDate);
                return scheduledDate >= resolvedWeekStartDate && scheduledDate <= weekEndDate;
            })
            .ToArray();
        var feedbackItems = (await _feedbackRepository.ListByChildSinceAsync(
                familyId,
                childId,
                ToUtcStart(resolvedWeekStartDate),
                cancellationToken))
            .Where(feedback => feedback.CreatedAt < ToUtcStart(weekEndDate.AddDays(1)))
            .ToArray();

        return LogResult(nameof(GetChildWeeklyReportAsync), familyId, new ChildWeeklyReportDto(
            childProfile.Id,
            ResolveChildName(childProfile),
            resolvedWeekStartDate,
            weekEndDate,
            CalculateAttendanceRate(attendanceRecords),
            CalculateTaskRate(taskItems, taskCompletions, childProfile.InternalId, resolvedWeekStartDate, weekEndDate),
            BuildFeedbackSummary(feedbackItems),
            BuildPillarScores(childProfile)));
    }

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!await IsReportReaderAsync(member, cancellationToken))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        return await BuildAttendanceSummaryAsync(familyId, childId, fromDate, toDate, cancellationToken);
    }

    private async Task<AttendanceSummaryDto> BuildAttendanceSummaryAsync(
        Guid familyId, Guid childId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var (resolvedFromDate, resolvedToDate) = await ResolveDateRangeAsync(fromDate, toDate, cancellationToken);
        var attendanceRecords = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
            familyId,
            childId,
            resolvedFromDate,
            resolvedToDate,
            cancellationToken);

        return LogResult(nameof(GetAttendanceSummaryAsync), familyId, new AttendanceSummaryDto(
            childId,
            resolvedFromDate,
            resolvedToDate,
            attendanceRecords.Count,
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Present),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Absent),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Late),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.LeftEarly),
            CalculateAttendanceRate(attendanceRecords),
            BuildHeatmap(attendanceRecords, resolvedFromDate, resolvedToDate)));
    }

    private async Task<WeeklyDigestDto> BuildWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
        var weekEndDate = weekStartDate.AddDays(6);
        var children = await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var taskCompletions = (await _taskCompletionRepository.ListByFamilyAsync(
                familyId,
                null,
                null,
                cancellationToken))
            .Where(taskCompletion =>
            {
                var scheduledDate = DateOnly.FromDateTime(taskCompletion.ScheduledDate);
                return scheduledDate >= weekStartDate && scheduledDate <= weekEndDate;
            })
            .ToArray();
        var feedbackItems = (await _feedbackRepository.ListByFamilyAsync(
                familyId,
                null,
                null,
                null,
                null,
                cancellationToken))
            .Where(feedback => feedback.CreatedAt >= ToUtcStart(weekStartDate)
                && feedback.CreatedAt < ToUtcStart(weekEndDate.AddDays(1)))
            .ToArray();
        var currentWeekScore = await CalculateFamilyTrendScoreAsync(
            familyId,
            children,
            taskItems,
            weekStartDate,
            weekEndDate,
            cancellationToken);
        var previousWeekScore = await CalculateFamilyTrendScoreAsync(
            familyId,
            children,
            taskItems,
            weekStartDate.AddDays(-7),
            weekStartDate.AddDays(-1),
            cancellationToken);
        var upcomingEvents = await _calendarEventRepository.ListByFamilyAsync(
            familyId,
            ToUtcStart(weekEndDate.AddDays(1)),
            ToUtcStart(weekEndDate.AddDays(8)),
            cancellationToken);
        var childDigestItems = new List<WeeklyDigestChildDto>(children.Count);

        foreach (var child in children)
        {
            var attendanceRecords = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
                familyId,
                child.Id,
                weekStartDate,
                weekEndDate,
                cancellationToken);
            var childTaskCompletions = taskCompletions
                .Where(taskCompletion => taskCompletion.ChildProfileId == child.InternalId)
                .ToArray();
            var childFeedback = feedbackItems
                .Where(feedback => feedback.ChildProfileId == child.InternalId)
                .ToArray();

            childDigestItems.Add(
                new WeeklyDigestChildDto(
                    child.Id,
                    ResolveChildName(child),
                    CalculateAttendanceRate(attendanceRecords),
                    CalculateTaskRate(taskItems, childTaskCompletions, child.InternalId, weekStartDate, weekEndDate),
                    childFeedback.Length));
        }

        // Level 2 — health + document sections (gracefully omitted when modules absent)
        IReadOnlyCollection<ExpiringDocumentItemDto>? expiringDocs = null;
        IReadOnlyCollection<HealthReminderItemDto>?   healthReminders = null;

        if (_vaultDocumentRepository is not null)
        {
            var expiring = await _vaultDocumentRepository.ListExpiringAsync(familyId, 30, cancellationToken);
            expiringDocs = expiring
                .Where(d => d.ExpiryDate.HasValue)
                .Select(d =>
                {
                    var expiry = DateOnly.FromDateTime(d.ExpiryDate!.Value);
                    return new ExpiringDocumentItemDto(
                        d.Id,
                        d.DocumentName,
                        d.Category.ToString(),
                        expiry,
                        (expiry.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days);
                })
                .OrderBy(d => d.DaysUntilExpiry)
                .ToArray();
        }

        if (_medicalRepository is not null)
        {
            healthReminders = await BuildHealthRemindersAsync(familyId, cancellationToken);
        }

        return new WeeklyDigestDto(
            family.Id,
            family.FamilyName,
            weekStartDate,
            weekEndDate,
            family.FamilyScore,
            ResolveTrendDirection(currentWeekScore, previousWeekScore),
            feedbackItems.Length,
            childDigestItems,
            upcomingEvents
                .Select(calendarEvent => new WeeklyDigestUpcomingEventDto(
                    calendarEvent.Id,
                    calendarEvent.EventTitle,
                    calendarEvent.StartDateTime,
                    calendarEvent.EndDateTime,
                    calendarEvent.EventType.ToString(),
                    calendarEvent.LinkedChildProfile?.Id))
                .ToArray(),
            expiringDocs,
            healthReminders);
    }

    private async Task<decimal> CalculateFamilyTrendScoreAsync(
        Guid familyId,
        IReadOnlyCollection<ChildProfile> children,
        IReadOnlyCollection<TaskItem> taskItems,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        if (children.Count == 0)
        {
            return 0m;
        }

        decimal attendanceRateTotal = 0m;
        decimal taskRateTotal = 0m;

        foreach (var child in children)
        {
            var attendanceRecords = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
                familyId,
                child.Id,
                fromDate,
                toDate,
                cancellationToken);
            var taskCompletions = (await _taskCompletionRepository.ListByFamilyAsync(
                    familyId,
                    child.Id,
                    null,
                    cancellationToken))
                .Where(taskCompletion =>
                {
                    var scheduledDate = DateOnly.FromDateTime(taskCompletion.ScheduledDate);
                    return scheduledDate >= fromDate && scheduledDate <= toDate;
                })
                .ToArray();

            attendanceRateTotal += CalculateAttendanceRate(attendanceRecords);
            taskRateTotal += CalculateTaskRate(taskItems, taskCompletions, child.InternalId, fromDate, toDate);
        }

        return Math.Round((attendanceRateTotal + taskRateTotal) / (children.Count * 2m), 2, MidpointRounding.AwayFromZero);
    }

    private static FeedbackSummaryDto BuildFeedbackSummary(IReadOnlyCollection<TeacherFeedback> feedbackItems)
    {
        return new FeedbackSummaryDto(
            feedbackItems.Count,
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Appreciation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Complaint),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Observation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.HomeworkIssue),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.UrgentEscalation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.WeeklySummary),
            feedbackItems
                .Where(feedback => !string.IsNullOrWhiteSpace(feedback.ParentResponseText))
                .OrderByDescending(feedback => feedback.AcknowledgedAt ?? feedback.UpdatedAt)
                .Select(feedback => feedback.ParentResponseText!.Trim())
                .FirstOrDefault());
    }

    private static IReadOnlyCollection<PillarScoreDto> BuildPillarScores(ChildProfile childProfile)
    {
        return new[]
        {
            new PillarScoreDto("Study", childProfile.StudyScore),
            new PillarScoreDto("Cleanliness", childProfile.CleanlinessScore),
            new PillarScoreDto("Discipline", childProfile.DisciplineScore),
            new PillarScoreDto("ScreenControl", childProfile.ScreenControlScore),
            new PillarScoreDto("Responsibility", childProfile.ResponsibilityScore)
        };
    }

    private static IReadOnlyCollection<AttendanceHeatmapEntryDto> BuildHeatmap(
        IReadOnlyCollection<AttendanceRecord> attendanceRecords,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var recordLookup = attendanceRecords
            .GroupBy(record => DateOnly.FromDateTime(record.Session!.ScheduledDate))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var heatmap = new List<AttendanceHeatmapEntryDto>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (!recordLookup.TryGetValue(date, out var records))
            {
                heatmap.Add(new AttendanceHeatmapEntryDto(date, "NoSession", 0));
                continue;
            }

            heatmap.Add(new AttendanceHeatmapEntryDto(
                date,
                ResolveHeatmapStatus(records.Select(record => record.Status)),
                records.Length));
        }

        return heatmap;
    }

    private static string ResolveHeatmapStatus(IEnumerable<AttendanceStatus> statuses)
    {
        if (statuses.Any(status => status == AttendanceStatus.Absent))
        {
            return AttendanceStatus.Absent.ToString();
        }

        if (statuses.Any(status => status == AttendanceStatus.Late))
        {
            return AttendanceStatus.Late.ToString();
        }

        if (statuses.Any(status => status == AttendanceStatus.LeftEarly))
        {
            return AttendanceStatus.LeftEarly.ToString();
        }

        return AttendanceStatus.Present.ToString();
    }

    private static decimal CalculateAttendanceRate(IReadOnlyCollection<AttendanceRecord> attendanceRecords)
    {
        if (attendanceRecords.Count == 0)
        {
            return 0m;
        }

        var presentCount = attendanceRecords.Count(record => record.Status == AttendanceStatus.Present);

        return Math.Round(presentCount * 100m / attendanceRecords.Count, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateTaskRate(
        IReadOnlyCollection<TaskItem> taskItems,
        IReadOnlyCollection<TaskCompletion> taskCompletions,
        long childProfileInternalId,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var assignedTaskCount = CountAssignedTasks(taskItems, childProfileInternalId, fromDate, toDate);

        if (assignedTaskCount == 0)
        {
            return 0m;
        }

        var completedTaskCount = taskCompletions.Count(taskCompletion =>
            taskCompletion.Status is TaskStatus.SubmittedForReview or TaskStatus.Approved);

        return Math.Round(completedTaskCount * 100m / assignedTaskCount, 2, MidpointRounding.AwayFromZero);
    }

    private static int CountAssignedTasks(
        IReadOnlyCollection<TaskItem> taskItems,
        long childProfileInternalId,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var total = 0;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            total += taskItems.Count(taskItem =>
                taskItem.IsActive
                && !taskItem.IsDeleted
                && !taskItem.IsSystemTemplate
                && IsTaskAssignedToChild(taskItem, childProfileInternalId)
                && IsActiveForDate(taskItem, date));
        }

        return total;
    }

    private static bool IsTaskAssignedToChild(TaskItem taskItem, long childProfileInternalId)
    {
        return !taskItem.ChildProfileId.HasValue || taskItem.ChildProfileId == childProfileInternalId;
    }

    private static bool IsActiveForDate(TaskItem taskItem, DateOnly targetDate)
    {
        var activeFromDate = DateOnly.FromDateTime(taskItem.ActiveFromDate);
        if (targetDate < activeFromDate)
        {
            return false;
        }

        if (taskItem.ActiveToDate.HasValue
            && targetDate > DateOnly.FromDateTime(taskItem.ActiveToDate.Value))
        {
            return false;
        }

        if (!taskItem.IsRecurring)
        {
            return targetDate == activeFromDate;
        }

        var recurringDays = JsonSerializer.Deserialize<int[]>(taskItem.RecurringDays) ?? Array.Empty<int>();
        var dayOfWeek = targetDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)targetDate.DayOfWeek;

        return recurringDays.Contains(dayOfWeek);
    }

    private static string ResolveTrendDirection(decimal currentWeekScore, decimal previousWeekScore)
    {
        if (currentWeekScore > previousWeekScore)
        {
            return "Up";
        }

        if (currentWeekScore < previousWeekScore)
        {
            return "Down";
        }

        return "Flat";
    }

    private static string ResolveChildName(ChildProfile childProfile)
    {
        return childProfile.User?.FullName
            ?? childProfile.FamilyMember?.DisplayName
            ?? childProfile.FamilyMember?.User?.FullName
            ?? "Child";
    }

    private static DateTime ToUtcStart(DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    private async Task<DateOnly> ResolveWeekStartDateAsync(DateOnly? weekStartDate, CancellationToken cancellationToken)
    {
        var resolvedWeekStartDate = weekStartDate ?? MostRecentMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        if (resolvedWeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["WeekStartDate"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Invalid_Week_Start, cancellationToken) }
                });
        }

        return resolvedWeekStartDate;
    }

    private async Task<(DateOnly FromDate, DateOnly ToDate)> ResolveDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        if (!fromDate.HasValue && !toDate.HasValue)
        {
            var weekStartDate = MostRecentMonday(DateOnly.FromDateTime(DateTime.UtcNow));

            return (weekStartDate, weekStartDate.AddDays(6));
        }

        var resolvedFromDate = fromDate ?? toDate!.Value;
        var resolvedToDate = toDate ?? fromDate!.Value;

        if (resolvedFromDate > resolvedToDate)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["DateRange"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Invalid_Date_Range, cancellationToken) }
                });
        }

        return (resolvedFromDate, resolvedToDate);
    }

    private static DateOnly MostRecentMonday(DateOnly referenceDate)
    {
        var dayOffset = ((int)referenceDate.DayOfWeek + 6) % 7;

        return referenceDate.AddDays(-dayOffset);
    }

    // ── Level 2 public methods ─────────────────────────────────────────────────

    public async Task<MonthlyFamilyReportDto> GetMonthlyFamilyReportAsync(
        Guid currentUserId, Guid familyId, int? year, int? month, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (!await IsReportReaderAsync(member, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        var (resolvedYear, resolvedMonth) = ResolvePeriod(year, month);
        var periodStart = new DateOnly(resolvedYear, resolvedMonth, 1);
        var periodEnd   = periodStart.AddMonths(1).AddDays(-1);
        var prevStart   = periodStart.AddMonths(-1);
        var prevEnd     = periodStart.AddDays(-1);

        var family   = await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
        var children = await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var allFeedback = (await _feedbackRepository.ListByFamilyAsync(familyId, null, null, null, null, cancellationToken))
            .Where(f => f.CreatedAt >= ToUtcStart(periodStart) && f.CreatedAt < ToUtcStart(periodEnd.AddDays(1)))
            .ToArray();

        var childSummaries = new List<MonthlyChildSummaryItemDto>(children.Count);

        foreach (var child in children)
        {
            var attendance = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
                familyId, child.Id, periodStart, periodEnd, cancellationToken);
            var prevAttendance = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
                familyId, child.Id, prevStart, prevEnd, cancellationToken);

            var completions = (await _taskCompletionRepository.ListByFamilyAsync(familyId, child.Id, null, cancellationToken))
                .Where(c =>
                {
                    var scheduledDate = DateOnly.FromDateTime(c.ScheduledDate);
                    return scheduledDate >= periodStart && scheduledDate <= periodEnd;
                }).ToArray();
            var prevCompletions = (await _taskCompletionRepository.ListByFamilyAsync(familyId, child.Id, null, cancellationToken))
                .Where(c =>
                {
                    var scheduledDate = DateOnly.FromDateTime(c.ScheduledDate);
                    return scheduledDate >= prevStart && scheduledDate <= prevEnd;
                }).ToArray();

            var coins = await _coinTransactionRepository.ListByChildAsync(familyId, child.Id, cancellationToken);
            var monthCoins = coins
                .Where(c => c.CreatedAt >= ToUtcStart(periodStart) && c.CreatedAt < ToUtcStart(periodEnd.AddDays(1)))
                .ToArray();

            var childFeedback = allFeedback.Where(f => f.ChildProfileId == child.InternalId).ToArray();
            var attRate     = CalculateAttendanceRate(attendance);
            var prevAttRate  = CalculateAttendanceRate(prevAttendance);
            var taskRate    = CalculateTaskRate(taskItems, completions, child.InternalId, periodStart, periodEnd);
            var prevTaskRate = CalculateTaskRate(taskItems, prevCompletions, child.InternalId, prevStart, prevEnd);

            childSummaries.Add(new MonthlyChildSummaryItemDto(
                child.Id,
                ResolveChildName(child),
                Math.Round(attRate, 2),
                Math.Round(attRate - prevAttRate, 2),
                Math.Round(taskRate, 2),
                Math.Round(taskRate - prevTaskRate, 2),
                childFeedback.Length,
                monthCoins.Where(c => c.TransactionType == "Earned").Sum(c => c.Amount),
                monthCoins.Where(c => c.TransactionType == "Spent").Sum(c => c.Amount)));
        }

        var resolvedCount = allFeedback.Count(f => f.AcknowledgedAt.HasValue);
        var resolutionRate = allFeedback.Length > 0
            ? Math.Round((decimal)resolvedCount / allFeedback.Length, 2)
            : 0m;

        IReadOnlyCollection<ExpiringDocumentItemDto> expiringDocs = Array.Empty<ExpiringDocumentItemDto>();
        if (_vaultDocumentRepository is not null)
        {
            var docs = await _vaultDocumentRepository.ListExpiringAsync(familyId, 30, cancellationToken);
            expiringDocs = docs.Where(d => d.ExpiryDate.HasValue).Select(d =>
            {
                var expiry = DateOnly.FromDateTime(d.ExpiryDate!.Value);
                return new ExpiringDocumentItemDto(d.Id, d.DocumentName, d.Category.ToString(), expiry,
                    (expiry.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days);
            }).OrderBy(d => d.DaysUntilExpiry).ToArray();
        }

        IReadOnlyCollection<HealthReminderItemDto> healthReminders = Array.Empty<HealthReminderItemDto>();
        if (_medicalRepository is not null)
            healthReminders = await BuildHealthRemindersAsync(familyId, cancellationToken);

        var headline = GenerateMonthlyHeadline(childSummaries, expiringDocs.Count, healthReminders.Count);

        return LogResult(nameof(GetMonthlyFamilyReportAsync), familyId, new MonthlyFamilyReportDto(
            family.Id, family.FamilyName,
            resolvedYear, resolvedMonth,
            childSummaries,
            allFeedback.Length,
            resolutionRate,
            expiringDocs,
            healthReminders,
            null,
            DateTime.UtcNow,
            headline));
    }

    public async Task<ChildMonthlySummaryDto> GetChildMonthlySummaryAsync(
        Guid currentUserId, Guid familyId, Guid childId, int? year, int? month, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (!await IsReportReaderAsync(member, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        return await BuildChildMonthlySummaryAsync(familyId, child, year, month, cancellationToken);
    }

    private async Task<ChildMonthlySummaryDto> BuildChildMonthlySummaryAsync(
        Guid familyId, ChildProfile child, int? year, int? month, CancellationToken cancellationToken)
    {
        var childId = child.Id;
        var (resolvedYear, resolvedMonth) = ResolvePeriod(year, month);
        var periodStart = new DateOnly(resolvedYear, resolvedMonth, 1);
        var periodEnd   = periodStart.AddMonths(1).AddDays(-1);

        var attendance = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
            familyId, childId, periodStart, periodEnd, cancellationToken);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var completions = (await _taskCompletionRepository.ListByFamilyAsync(familyId, childId, null, cancellationToken))
            .Where(c =>
            {
                var scheduledDate = DateOnly.FromDateTime(c.ScheduledDate);
                return scheduledDate >= periodStart && scheduledDate <= periodEnd;
            }).ToArray();
        var feedback = (await _feedbackRepository.ListByChildSinceAsync(familyId, childId, ToUtcStart(periodStart), cancellationToken))
            .Where(f => f.CreatedAt < ToUtcStart(periodEnd.AddDays(1))).ToArray();
        var coins = (await _coinTransactionRepository.ListByChildAsync(familyId, childId, cancellationToken))
            .Where(c => c.CreatedAt >= ToUtcStart(periodStart) && c.CreatedAt < ToUtcStart(periodEnd.AddDays(1))).ToArray();

        var assignedCount = CountAssignedTasks(taskItems, child.InternalId, periodStart, periodEnd);
        var approvedCount = completions.Count(c => c.Status == TaskStatus.Approved);
        var taskRate      = assignedCount > 0 ? Math.Round(approvedCount * 100m / assignedCount, 2) : 0m;
        var attRate       = CalculateAttendanceRate(attendance);

        var feedbackByType = feedback
            .GroupBy(f => f.FeedbackType.ToString())
            .ToDictionary(g => g.Key, g => g.Count()) as IReadOnlyDictionary<string, int>;

        // Pillar snapshot — current month only until ChildPillarScoreHistory table is seeded
        var currentSnapshot = new PillarScoreSnapshotDto(
            periodStart,
            child.StudyScore,
            child.CleanlinessScore,
            child.DisciplineScore,
            child.ScreenControlScore,
            child.ResponsibilityScore);

        var narrative = GenerateChildNarrative(ResolveChildName(child), attRate, taskRate,
            coins.Where(c => c.TransactionType == "Earned").Sum(c => c.Amount),
            currentSnapshot);

        return LogResult(nameof(GetChildMonthlySummaryAsync), familyId, new ChildMonthlySummaryDto(
            child.Id,
            ResolveChildName(child),
            resolvedYear,
            resolvedMonth,
            Math.Round(attRate, 2),
            attendance.Count,
            attendance.Count(r => r.Status == AttendanceStatus.Present),
            attendance.Count(r => r.Status == AttendanceStatus.Absent),
            taskRate,
            assignedCount,
            approvedCount,
            feedback.Length,
            feedbackByType,
            coins.Where(c => c.TransactionType == "Earned").Sum(c => c.Amount),
            coins.Where(c => c.TransactionType == "Spent").Sum(c => c.Amount),
            new[] { currentSnapshot },
            narrative));
    }

    public async Task<IReadOnlyCollection<ExpiringDocumentItemDto>> GetDocumentExpiryReportAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (!await IsReportReaderAsync(member, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (_vaultDocumentRepository is null)
            return LogResult(nameof(GetDocumentExpiryReportAsync), familyId, Array.Empty<ExpiringDocumentItemDto>());

        var docs = await _vaultDocumentRepository.ListExpiringAsync(familyId, 90, cancellationToken);

        return LogResult(nameof(GetDocumentExpiryReportAsync), familyId, docs
            .Where(d => d.ExpiryDate.HasValue)
            .Select(d =>
            {
                var expiry = DateOnly.FromDateTime(d.ExpiryDate!.Value);
                return new ExpiringDocumentItemDto(
                    d.Id, d.DocumentName, d.Category.ToString(), expiry,
                    (expiry.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days);
            })
            .OrderBy(d => d.DaysUntilExpiry)
            .ToArray());
    }

    public async Task<IReadOnlyCollection<HealthReminderItemDto>> GetHealthReminderSummaryAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (!await IsReportReaderAsync(member, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (_medicalRepository is null)
            return LogResult(nameof(GetHealthReminderSummaryAsync), familyId, Array.Empty<HealthReminderItemDto>());

        return LogResult(nameof(GetHealthReminderSummaryAsync), familyId, await BuildHealthRemindersAsync(familyId, cancellationToken));
    }

    public async Task<ReportExportDto> ExportReportAsync(
        Guid currentUserId, Guid familyId, ExportReportRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (member.Role is not (UserRole.Parent or UserRole.FamilyAdmin) ||
            _exportPermissionService is null ||
            !await _exportPermissionService.CheckAsync(member.Role, FamilyFirstModule.Reports,
                FamilyFirstPermission.View, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (!string.Equals(request.Format, "PDF", StringComparison.OrdinalIgnoreCase))
            throw await ExportValidationAsync(FamilyFirstErrorCode.Feature_Not_Available, cancellationToken, 422);

        if (request.ReportType is not ("MonthlyFamily" or "ChildMonthly" or "WeeklyDigest" or "AttendanceSummary" or "Finance"))
            throw await ExportValidationAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken);

        var start = await ResolveExportPeriodAsync(request, cancellationToken);
        var requiresChild = request.ReportType is "ChildMonthly" or "AttendanceSummary";
        if (requiresChild && (request.ChildId is null || request.ChildId == Guid.Empty))
            throw await ExportValidationAsync(FamilyFirstErrorCode.Missing_Parameters, cancellationToken);
        if (!requiresChild && request.ChildId.HasValue)
            throw await ExportValidationAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken);

        var child = request.ChildId is Guid childId
            ? await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken)
            : null;
        if (!_reportExportStorage.IsConfigured)
            throw await ExportValidationAsync(FamilyFirstErrorCode.Feature_Not_Available, cancellationToken, 422);

        StoredReportPdf stored;
        try
        {
            var content = await BuildExportContentAsync(currentUserId, familyId, request, start, child, cancellationToken);
            stored = await _reportExportStorage.StoreAsync(familyId, content, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordFailedExportAsync(member, child, request, currentUserId, exception);
            throw;
        }

        try
        {
            await _reportExports.AddAsync(new ReportExport
            {
                Id = Guid.Empty,
                FamilyId = member.FamilyId,
                RequestedByUserId = member.UserId,
                ChildProfileId = child?.InternalId,
                ReportType = request.ReportType,
                Period = request.Period,
                Format = "PDF",
                Status = "Ready",
                // Persist the canonical object URL only: the presigned URL is a short-lived bearer link and is
                // returned to the caller, never stored.
                DownloadUrl = stored.DownloadUrl.Split('?')[0],
                ExpiresAtUtc = stored.ExpiresAtUtc,
                CreatedBy = currentUserId.ToString("D"),
            }, cancellationToken);
        }
        catch
        {
            try
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _reportExportStorage.DeleteAsync(stored.ObjectKey, cleanup.Token);
            }
            catch
            {
                LogCompletion("ReportExportCleanup", familyId, hasResult: false);
            }
            throw;
        }

        return LogResult(nameof(ExportReportAsync), familyId, new ReportExportDto(stored.DownloadUrl, stored.ExpiresAtUtc, "PDF"));
    }

    // Best effort: a Failed row makes generation failures visible in tblReportExport. It must never mask the original
    // exception, so its own failure is swallowed. ErrorMessage holds the exception type only (no PII / payload).
    private async Task RecordFailedExportAsync(
        FamilyMember member, ChildProfile? child, ExportReportRequest request, Guid currentUserId, Exception exception)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _reportExports.AddAsync(new ReportExport
            {
                Id = Guid.Empty,
                FamilyId = member.FamilyId,
                RequestedByUserId = member.UserId,
                ChildProfileId = child?.InternalId,
                ReportType = request.ReportType,
                Period = request.Period,
                Format = "PDF",
                Status = "Failed",
                ErrorMessage = exception.GetType().Name,
                CreatedBy = currentUserId.ToString("D"),
            }, timeout.Token);
        }
        catch
        {
            LogCompletion("ReportExportFailureRecord", null, hasResult: false);
        }
    }

    private async Task<ValidationException> ExportValidationAsync(
        FamilyFirstErrorCode code, CancellationToken cancellationToken, int statusCode = 400)
    {
        var message = await _errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);
        return new ValidationException(new Dictionary<string, string[]> { [code.ToString()] = [message] }, statusCode);
    }

    private async Task<DateOnly> ResolveExportPeriodAsync(ExportReportRequest request, CancellationToken cancellationToken)
    {
        var period = request.Period;
        if (request.ReportType == "WeeklyDigest")
        {
            if (period is { Length: 8 } && period.AsSpan(4, 2).SequenceEqual("-W") &&
                int.TryParse(period.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) &&
                int.TryParse(period.AsSpan(6, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var week) &&
                year is >= 2 and <= 9998 && week >= 1 && week <= ISOWeek.GetWeeksInYear(year))
                return DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
        }
        else if (period is { Length: 7 } &&
            DateOnly.TryParseExact(period + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            start.Year is >= 2 and <= 9998)
        {
            return start;
        }

        throw await ExportValidationAsync(FamilyFirstErrorCode.Invalid_Date_Range, cancellationToken);
    }

    private async Task<ReportPdfContent> BuildExportContentAsync(
        Guid userId, Guid familyId, ExportReportRequest request, DateOnly start, ChildProfile? child,
        CancellationToken cancellationToken)
    {
        var sections = new List<ReportPdfSection>();
        var title = "Monthly Family Report";
        switch (request.ReportType)
        {
            case "MonthlyFamily":
                var family = await GetMonthlyFamilyReportAsync(userId, familyId, start.Year, start.Month, cancellationToken);
                sections.Add(new(family.FamilyName, [family.NarrativeHeadline, $"Feedback received: {family.TotalFeedbackCount}"]));
                sections.AddRange(family.Children.Select(item => new ReportPdfSection(item.ChildName,
                    [$"Attendance: {item.AttendanceRate:F0}%", $"Tasks completed: {item.TaskRate:F0}%",
                        $"Feedback received: {item.FeedbackCount}", $"Coins earned: {item.CoinsEarned}", $"Coins spent: {item.CoinsSpent}"])));
                break;
            case "ChildMonthly":
                var monthly = await BuildChildMonthlySummaryAsync(familyId, child!, start.Year, start.Month, cancellationToken);
                title = "Child Monthly Report";
                sections.Add(new(monthly.ChildName, [monthly.NarrativeSummary,
                    $"Attendance: {monthly.AttendanceRate:F0}% across {monthly.AttendanceSessions} sessions",
                    $"Tasks approved: {monthly.TaskApprovedCount} of {monthly.TaskAssignedCount}",
                    $"Feedback received: {monthly.FeedbackCount}", $"Coins earned: {monthly.CoinsEarned}",
                    $"Coins spent: {monthly.CoinsSpent}"]));
                break;
            case "WeeklyDigest":
                var weekly = await GetWeeklyDigestAsync(userId, familyId, start, cancellationToken);
                title = "Weekly Family Digest";
                sections.Add(new(weekly.FamilyName, [$"Family score: {weekly.FamilyScore}", $"Feedback received: {weekly.TotalFeedbackCount}"]));
                sections.AddRange(weekly.Children.Select(item => new ReportPdfSection(item.ChildName,
                    [$"Attendance: {item.AttendanceRate:F0}%", $"Tasks completed: {item.TaskRate:F0}%",
                        $"Feedback received: {item.FeedbackCount}"])));
                break;
            case "AttendanceSummary":
                var attendance = await BuildAttendanceSummaryAsync(familyId, child!.Id, start,
                    start.AddMonths(1).AddDays(-1), cancellationToken);
                title = "Attendance Summary";
                sections.Add(new(ResolveChildName(child), [$"Attendance rate: {attendance.AttendanceRate:F0}%",
                    $"Sessions: {attendance.TotalSessions}", $"Present: {attendance.PresentCount}",
                    $"Absent: {attendance.AbsentCount}", $"Late: {attendance.LateCount}", $"Left early: {attendance.LeftEarlyCount}"]));
                sections.Add(new("Session days", attendance.Heatmap.Where(entry => entry.SessionCount > 0)
                    .Select(entry => $"{entry.Date:yyyy-MM-dd}   {entry.Status}   {entry.SessionCount}").ToArray()));
                break;
            case "Finance":
                if (_financeService is null)
                    throw await ExportValidationAsync(FamilyFirstErrorCode.Feature_Not_Available, cancellationToken, 422);
                title = "Monthly Finance Report";
                sections.Add(await BuildFinanceExportSummaryAsync(userId, familyId, start, cancellationToken));
                break;
        }

        return new ReportPdfContent(title, request.Period, sections);
    }

    private async Task<ReportPdfSection> BuildFinanceExportSummaryAsync(
        Guid userId, Guid familyId, DateOnly start, CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var from = ToUtcStart(start);
        var through = ToUtcStart(start.AddMonths(1)).AddTicks(-1);
        decimal income = 0;
        decimal spend = 0;
        int total;
        int page = 0;
        do
        {
            var result = await _financeService!.ListTransactionsAsync(
                userId, familyId, null, null, from, through, ++page, pageSize, cancellationToken);
            income += result.Items.Where(item => item.TransactionType == "Credit").Sum(item => item.Amount);
            spend += result.Items.Where(item => item.TransactionType == "Debit").Sum(item => item.Amount);
            total = result.TotalCount;
        } while ((long)page * pageSize < total);

        return new("Visible transactions", [$"Income: {income:F2}", $"Spending: {spend:F2}", $"Net: {income - spend:F2}"]);
    }

    public async Task<PaginatedList<ReportArchiveItemDto>> GetReportArchiveAsync(
        Guid currentUserId, Guid familyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        if (!await IsReportReaderAsync(member, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        var (archives, totalCount) = await _archiveRepository.ListByFamilyAsync(
            familyId, page, pageSize, cancellationToken);

        var items = archives
            .Select(a =>
            {
                var (familyScore, childCount) = ExtractDigestSummary(a.DigestContentJson);
                return new ReportArchiveItemDto(
                    a.Id,
                    DateOnly.FromDateTime(a.WeekStartDate),
                    DateOnly.FromDateTime(a.WeekStartDate).AddDays(6),
                    a.GeneratedAt,
                    familyScore,
                    childCount,
                    a.ShareableImageUrl);
            })
            .ToArray();

        return LogResult(nameof(GetReportArchiveAsync), familyId, new PaginatedList<ReportArchiveItemDto>(items, totalCount, page, pageSize));
    }

    private static (int FamilyScore, int ChildCount) ExtractDigestSummary(string digestContentJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(digestContentJson);
            var root = doc.RootElement;
            var familyScore = root.TryGetProperty("familyScore", out var scoreEl) ? scoreEl.GetInt32() : 0;
            var childCount  = root.TryGetProperty("children", out var childrenEl)
                ? childrenEl.GetArrayLength()
                : 0;
            return (familyScore, childCount);
        }
        catch
        {
            return (0, 0);
        }
    }

    // ── Level 2 private helpers ────────────────────────────────────────────────

    private async Task<IReadOnlyCollection<HealthReminderItemDto>> BuildHealthRemindersAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        var reminders = new List<HealthReminderItemDto>();
        var profiles  = await _medicalRepository!.ListByFamilyAsync(familyId, cancellationToken);

        foreach (var profile in profiles)
        {
            var vaccinations   = await _medicalRepository.ListVaccinationsAsync(profile.Id, cancellationToken);
            var prescriptions  = await _medicalRepository.ListActivePrescriptionsAsync(profile.Id, cancellationToken);
            var memberName     = profile.FamilyMember?.DisplayName ?? "Member";

            reminders.AddRange(vaccinations
                .Where(v => v.Status == VaccinationStatus.Due || v.Status == VaccinationStatus.Overdue)
                .Select(v => new HealthReminderItemDto(
                    profile.FamilyMember?.Id ?? Guid.Empty,
                    memberName,
                    "Vaccination",
                    v.VaccineName,
                    v.DueDate.HasValue ? DateOnly.FromDateTime(v.DueDate.Value) : null)));

            reminders.AddRange(prescriptions
                .Where(p => p.EndDate.HasValue && p.EndDate.Value <= DateTime.UtcNow.AddDays(14))
                .Select(p => new HealthReminderItemDto(
                    profile.FamilyMember?.Id ?? Guid.Empty,
                    memberName,
                    "Prescription",
                    $"{p.MedicationName} — refill due soon",
                    p.EndDate.HasValue ? DateOnly.FromDateTime(p.EndDate.Value) : null)));
        }

        return reminders.OrderBy(r => r.DueDate ?? DateOnly.MaxValue).ToArray();
    }

    private static (int Year, int Month) ResolvePeriod(int? year, int? month)
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        return (year ?? previous.Year, month ?? previous.Month);
    }

    private static string GenerateMonthlyHeadline(
        IReadOnlyCollection<MonthlyChildSummaryItemDto> children,
        int expiringDocs,
        int healthReminders)
    {
        if (children.Count == 0)
            return "Your monthly family report is ready.";

        var bestChild = children.MaxBy(c => c.AttendanceRate + c.TaskRate);
        if (bestChild is not null && bestChild.AttendanceRate >= 90m)
            return $"{bestChild.ChildName} had an outstanding month — {bestChild.AttendanceRate:F0}% attendance!";

        if (expiringDocs > 0)
            return $"Great month overall — {expiringDocs} document{(expiringDocs > 1 ? "s" : "")} need{(expiringDocs == 1 ? "s" : "")} renewal soon.";

        if (healthReminders > 0)
            return $"Good family week — {healthReminders} health reminder{(healthReminders > 1 ? "s" : "")} need{(healthReminders == 1 ? "s" : "")} attention.";

        var avgAtt = children.Average(c => (double)c.AttendanceRate);
        return avgAtt >= 80
            ? "Your family had a strong month. Keep it up!"
            : "A steady month for your family. There's room to grow next month.";
    }

    private static string GenerateChildNarrative(
        string childName,
        decimal attRate,
        decimal taskRate,
        int coinsEarned,
        PillarScoreSnapshotDto pillar)
    {
        var parts = new List<string>();
        if (attRate >= 90m) parts.Add($"{childName} had excellent attendance this month");
        else if (attRate >= 70m) parts.Add($"{childName} maintained good attendance");
        else parts.Add($"{childName} missed some sessions this month");

        if (taskRate >= 80m) parts.Add("completed tasks consistently");
        else if (taskRate >= 50m) parts.Add("made progress on tasks");

        if (coinsEarned > 0) parts.Add($"earned {coinsEarned} coins");

        var highestPillar = new[]
        {
            ("Study", pillar.StudyScore),
            ("Discipline", pillar.DisciplineScore),
            ("Responsibility", pillar.ResponsibilityScore),
            ("Cleanliness", pillar.CleanlinessScore),
            ("Screen Control", pillar.ScreenControlScore),
        }.MaxBy(p => p.Item2);

        if (highestPillar.Item2 >= 15)
            parts.Add($"and their {highestPillar.Item1} pillar is at its highest level");

        return string.Join(", ", parts) + ".";
    }

    // §18.3 "Level 2 Reports: FA ✓, P ✓" — both roles, and the DB matrix (Reports View) must agree.
    // Fails closed when the permission service is not registered.
    private async Task<bool> IsReportReaderAsync(FamilyMember member, CancellationToken cancellationToken) =>
        member.Role is UserRole.Parent or UserRole.FamilyAdmin
        && _exportPermissionService is not null
        && await _exportPermissionService.CheckAsync(member.Role, FamilyFirstModule.Reports, FamilyFirstPermission.View, cancellationToken);

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);

        if (member is null)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(
        Guid childId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
        return await _childProfileRepository.GetByIdForFamilyAsync(childId, family.InternalId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }

    private Task<string> GetMessageAsync(FamilyFirstErrorCode code, CancellationToken cancellationToken) =>
        _errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);

    private T LogResult<T>(string method, Guid? familyId, T result)
    {
        LogCompletion(method, familyId, result is not null);
        return result;
    }

    private void LogCompletion(string method, Guid? familyId, bool hasResult = true) =>
        _apiLogService.Log(method, System.Text.Json.JsonSerializer.Serialize(new { FamilyId = familyId }),
            System.Text.Json.JsonSerializer.Serialize(new { HasResult = hasResult }));
}
