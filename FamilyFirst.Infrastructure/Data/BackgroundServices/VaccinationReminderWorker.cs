using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class VaccinationReminderWorker : BackgroundService
{
    private const int DueSoonDays = 14;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VaccinationReminderWorker> _logger;

    public VaccinationReminderWorker(IServiceScopeFactory scopeFactory, ILogger<VaccinationReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VaccinationReminderWorker: unhandled error.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicalRepo          = scope.ServiceProvider.GetRequiredService<IMedicalRepository>();
        var notificationRepo     = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var memberRepo           = scope.ServiceProvider.GetRequiredService<IFamilyMemberRepository>();

        // Send 14-day ahead reminders
        var dueSoon = await medicalRepo.GetVaccinationsDueForReminderAsync(DueSoonDays, cancellationToken);
        foreach (var vaccination in dueSoon)
        {
            await SendNotificationToParentsAsync(
                vaccination.FamilyId,
                $"Vaccination due in {DueSoonDays} days: {vaccination.VaccineName}",
                $"{vaccination.VaccineName} is due on {vaccination.DueDate:dd MMM yyyy}. Book an appointment.",
                NotificationPriority.Normal,
                notificationRepo, memberRepo, cancellationToken);

            _logger.LogInformation(
                "VaccinationReminderWorker: due reminder sent. Vaccination={VaccinationId} Family={FamilyId}.",
                vaccination.Id, vaccination.FamilyId);
        }

        // Mark overdue vaccinations and send urgent alerts
        var overdue = await medicalRepo.GetOverdueVaccinationsAsync(cancellationToken);
        foreach (var vaccination in overdue)
        {
            vaccination.Status = VaccinationStatus.Overdue;
            await medicalRepo.UpdateVaccinationAsync(vaccination, cancellationToken);

            await SendNotificationToParentsAsync(
                vaccination.FamilyId,
                $"OVERDUE vaccination: {vaccination.VaccineName}",
                $"{vaccination.VaccineName} was due on {vaccination.DueDate:dd MMM yyyy} and has not been recorded. Please schedule immediately.",
                NotificationPriority.Urgent,
                notificationRepo, memberRepo, cancellationToken);

            _logger.LogInformation(
                "VaccinationReminderWorker: overdue alert sent. Vaccination={VaccinationId} Family={FamilyId}.",
                vaccination.Id, vaccination.FamilyId);
        }

        // Auto-archive prescriptions past their EndDate
        var medicalRepoForPrescriptions = scope.ServiceProvider.GetRequiredService<IMedicalRepository>();
        var prescriptionsToArchive = await medicalRepoForPrescriptions.GetPrescriptionsDueForArchiveAsync(cancellationToken);
        foreach (var prescription in prescriptionsToArchive)
        {
            prescription.IsArchived  = true;
            prescription.ArchivedAt  = DateTime.UtcNow;
            await medicalRepoForPrescriptions.UpdatePrescriptionAsync(prescription, cancellationToken);
        }

        if (prescriptionsToArchive.Count > 0)
        {
            _logger.LogInformation(
                "VaccinationReminderWorker: {Count} prescriptions auto-archived.",
                prescriptionsToArchive.Count);
        }
    }

    private static async Task SendNotificationToParentsAsync(
        Guid familyId,
        string title,
        string body,
        NotificationPriority priority,
        INotificationRepository notificationRepo,
        IFamilyMemberRepository memberRepo,
        CancellationToken cancellationToken)
    {
        var members = await memberRepo.ListActiveByFamilyAsync(familyId, cancellationToken);
        var parents = members.Where(m => m.Role is UserRole.Parent or UserRole.FamilyAdmin).ToArray();

        if (parents.Length == 0) return;

        var notifications = parents.Select(p => new Notification
        {
            RecipientUserId = p.UserId,
            FamilyId        = familyId,
            Title           = title,
            Body            = body,
            Channel         = NotificationChannel.Push,
            Priority        = priority,
            IsRead          = false,
            ScheduledFor    = DateTime.UtcNow
        }).ToArray();

        await notificationRepo.AddRangeAsync(notifications, cancellationToken);
    }
}
