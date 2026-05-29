using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
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

    public ReportService(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        IAttendanceRecordRepository attendanceRecordRepository,
        ITaskItemRepository taskItemRepository,
        ITaskCompletionRepository taskCompletionRepository,
        IFeedbackRepository feedbackRepository,
        ICalendarEventRepository calendarEventRepository)
    {
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _attendanceRecordRepository = attendanceRecordRepository;
        _taskItemRepository = taskItemRepository;
        _taskCompletionRepository = taskCompletionRepository;
        _feedbackRepository = feedbackRepository;
        _calendarEventRepository = calendarEventRepository;
    }

    public async Task<WeeklyDigestDto> GetWeeklyDigestAsync(
        Guid currentUserId,
        Guid familyId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Parent or FamilyAdmin role is required.");
        }

        return await BuildWeeklyDigestAsync(
            familyId,
            ResolveWeekStartDate(weekStartDate),
            cancellationToken);
    }

    public Task<WeeklyDigestDto> GenerateWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        return BuildWeeklyDigestAsync(familyId, ResolveWeekStartDate(weekStartDate), cancellationToken);
    }

    public async Task<ChildWeeklyReportDto> GetChildWeeklyReportAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException("Parent role is required.");
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var resolvedWeekStartDate = ResolveWeekStartDate(weekStartDate);
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
            .Where(taskCompletion => taskCompletion.ScheduledDate >= resolvedWeekStartDate
                && taskCompletion.ScheduledDate <= weekEndDate)
            .ToArray();
        var feedbackItems = (await _feedbackRepository.ListByChildSinceAsync(
                familyId,
                childId,
                ToUtcStart(resolvedWeekStartDate),
                cancellationToken))
            .Where(feedback => feedback.CreatedAt < ToUtcStart(weekEndDate.AddDays(1)))
            .ToArray();

        return new ChildWeeklyReportDto(
            childProfile.Id,
            ResolveChildName(childProfile),
            resolvedWeekStartDate,
            weekEndDate,
            CalculateAttendanceRate(attendanceRecords),
            CalculateTaskRate(taskItems, taskCompletions, childProfile.Id, resolvedWeekStartDate, weekEndDate),
            BuildFeedbackSummary(feedbackItems),
            BuildPillarScores(childProfile));
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

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException("Parent role is required.");
        }

        await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var (resolvedFromDate, resolvedToDate) = ResolveDateRange(fromDate, toDate);
        var attendanceRecords = await _attendanceRecordRepository.ListByChildAndDateRangeAsync(
            familyId,
            childId,
            resolvedFromDate,
            resolvedToDate,
            cancellationToken);

        return new AttendanceSummaryDto(
            childId,
            resolvedFromDate,
            resolvedToDate,
            attendanceRecords.Count,
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Present),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Absent),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.Late),
            attendanceRecords.Count(record => record.Status == AttendanceStatus.LeftEarly),
            CalculateAttendanceRate(attendanceRecords),
            BuildHeatmap(attendanceRecords, resolvedFromDate, resolvedToDate));
    }

    private async Task<WeeklyDigestDto> BuildWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Family), familyId);
        var weekEndDate = weekStartDate.AddDays(6);
        var children = await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var taskCompletions = (await _taskCompletionRepository.ListByFamilyAsync(
                familyId,
                null,
                null,
                cancellationToken))
            .Where(taskCompletion => taskCompletion.ScheduledDate >= weekStartDate
                && taskCompletion.ScheduledDate <= weekEndDate)
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
                .Where(taskCompletion => taskCompletion.ChildProfileId == child.Id)
                .ToArray();
            var childFeedback = feedbackItems
                .Where(feedback => feedback.ChildProfileId == child.Id)
                .ToArray();

            childDigestItems.Add(
                new WeeklyDigestChildDto(
                    child.Id,
                    ResolveChildName(child),
                    CalculateAttendanceRate(attendanceRecords),
                    CalculateTaskRate(taskItems, childTaskCompletions, child.Id, weekStartDate, weekEndDate),
                    childFeedback.Length));
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
                    calendarEvent.LinkedChildProfileId))
                .ToArray());
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
                .Where(taskCompletion => taskCompletion.ScheduledDate >= fromDate
                    && taskCompletion.ScheduledDate <= toDate)
                .ToArray();

            attendanceRateTotal += CalculateAttendanceRate(attendanceRecords);
            taskRateTotal += CalculateTaskRate(taskItems, taskCompletions, child.Id, fromDate, toDate);
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
            .GroupBy(record => record.Session!.ScheduledDate)
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
        Guid childProfileId,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var assignedTaskCount = CountAssignedTasks(taskItems, childProfileId, fromDate, toDate);

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
        Guid childProfileId,
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
                && IsTaskAssignedToChild(taskItem, childProfileId)
                && IsActiveForDate(taskItem, date));
        }

        return total;
    }

    private static bool IsTaskAssignedToChild(TaskItem taskItem, Guid childProfileId)
    {
        return !taskItem.ChildProfileId.HasValue || taskItem.ChildProfileId == childProfileId;
    }

    private static bool IsActiveForDate(TaskItem taskItem, DateOnly targetDate)
    {
        if (targetDate < taskItem.ActiveFromDate)
        {
            return false;
        }

        if (taskItem.ActiveToDate.HasValue && targetDate > taskItem.ActiveToDate.Value)
        {
            return false;
        }

        if (!taskItem.IsRecurring)
        {
            return targetDate == taskItem.ActiveFromDate;
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

    private static DateOnly ResolveWeekStartDate(DateOnly? weekStartDate)
    {
        var resolvedWeekStartDate = weekStartDate ?? MostRecentMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        if (resolvedWeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["WeekStartDate"] = new[] { "weekStartDate must be a Monday." }
                });
        }

        return resolvedWeekStartDate;
    }

    private static (DateOnly FromDate, DateOnly ToDate) ResolveDateRange(DateOnly? fromDate, DateOnly? toDate)
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
                    ["DateRange"] = new[] { "fromDate must be less than or equal to toDate." }
                });
        }

        return (resolvedFromDate, resolvedToDate);
    }

    private static DateOnly MostRecentMonday(DateOnly referenceDate)
    {
        var dayOffset = ((int)referenceDate.DayOfWeek + 6) % 7;

        return referenceDate.AddDays(-dayOffset);
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);

        if (member is null)
        {
            throw new ForbiddenAccessException("User is not an active member of this family.");
        }

        return member;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(
        Guid childId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var childProfile = await _childProfileRepository.GetByIdAsync(childId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChildProfile), childId);

        if (childProfile.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childId);
        }

        return childProfile;
    }
}
