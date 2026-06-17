using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class BirthdayEventGeneratorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BirthdayEventGeneratorWorker> _logger;

    public BirthdayEventGeneratorWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BirthdayEventGeneratorWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = now.Date.AddDays(1);
            var delay = nextRunUtc - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                await GenerateBirthdayEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Birthday event generator worker failed.");
            }
        }
    }

    private async Task GenerateBirthdayEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var childProfileRepository = scope.ServiceProvider.GetRequiredService<IChildProfileRepository>();
        var familyMemberRepository = scope.ServiceProvider.GetRequiredService<IFamilyMemberRepository>();
        var calendarEventRepository = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();
        var eventReminderRepository = scope.ServiceProvider.GetRequiredService<IEventReminderRepository>();
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var profiles = await childProfileRepository.ListByBirthdayAsync(targetDate.Month, targetDate.Day, cancellationToken);

        foreach (var profile in profiles)
        {
            if (await calendarEventRepository.GetBirthdayEventAsync(
                    profile.Family?.Id ?? Guid.Empty,
                    profile.Id,
                    targetDate,
                    cancellationToken) is not null)
            {
                continue;
            }

            var familyMembers = await familyMemberRepository.ListActiveByFamilyAsync(profile.Family?.Id ?? Guid.Empty, cancellationToken);
            var creator = familyMembers
                .OrderBy(member => member.Role == UserRole.FamilyAdmin ? 0 : member.Role == UserRole.Parent ? 1 : 2)
                .ThenBy(member => member.JoinedAt)
                .FirstOrDefault(member => member.Role is UserRole.FamilyAdmin or UserRole.Parent);

            if (creator is null)
            {
                _logger.LogWarning(
                    "Birthday event skipped for child {ChildProfileId} because no parent or family admin exists.",
                    profile.Id);
                continue;
            }

            var childName = profile.FamilyMember?.DisplayName
                ?? profile.User?.FullName
                ?? profile.FamilyMember?.User?.FullName
                ?? "Child";
            var eventDateTime = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var calendarEvent = new CalendarEvent
            {
                FamilyId = profile.FamilyId,
                CreatedByUserId = creator.UserId,
                EventTitle = $"{childName}'s Birthday",
                EventType = EventType.Birthday,
                Description = $"Auto-generated birthday reminder for {childName}.",
                StartDateTime = eventDateTime,
                EndDateTime = eventDateTime,
                IsAllDay = true,
                VisibilityScope = "Family",
                LinkedChildProfileId = profile.InternalId,
                IsRecurring = false,
                IsActive = true
            };

            await eventReminderRepository.AddEventGraphAsync(calendarEvent, Array.Empty<EventReminder>(), cancellationToken);
        }
    }
}
