using FamilyFirst.Application.Services.Implementations;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Infrastructure.Data;
using FamilyFirst.Infrastructure.Data.BackgroundServices;
using FamilyFirst.Infrastructure.Data.Repositories.Implementations;
using FamilyFirst.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        services.AddDbContext<FamilyFirstDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IChildService, ChildService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICommentTemplateService, CommentTemplateService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ICoinService, CoinService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IFamilyAdminService, FamilyAdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyMemberRepository, FamilyMemberRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IChildProfileRepository, ChildProfileRepository>();
        services.AddScoped<ITeacherProfileRepository, TeacherProfileRepository>();
        services.AddScoped<ITeacherChildAssignmentRepository, TeacherChildAssignmentRepository>();
        services.AddScoped<IAttendanceSessionRepository, AttendanceSessionRepository>();
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICommentTemplateRepository, CommentTemplateRepository>();
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<ITaskCompletionRepository, TaskCompletionRepository>();
        services.AddScoped<ICoinTransactionRepository, CoinTransactionRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IRewardRepository, RewardRepository>();
        services.AddScoped<IRewardRedemptionRepository, RewardRedemptionRepository>();
        services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
        services.AddScoped<IEventReminderRepository, EventReminderRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IFamilyAdminConfigRepository, FamilyAdminConfigRepository>();
        services.AddScoped<IS3StorageService, S3StorageService>();
        services.AddScoped<IDocumentVaultService, DocumentVaultService>();
        services.AddScoped<IVaultDocumentRepository, VaultDocumentRepository>();
        services.AddScoped<IVaultStorageService, VaultStorageService>();
        services.AddHostedService<VaultExpiryWorker>();
        services.AddScoped<IMedicalService, MedicalService>();
        services.AddScoped<IMedicalRepository, MedicalRepository>();
        services.AddHostedService<VaccinationReminderWorker>();
        services.AddScoped<ISafetyService, SafetyService>();
        services.AddScoped<ISafetyRepository, SafetyRepository>();
        services.AddHostedService<SafetyWorker>();
        services.AddScoped<IWeeklyDigestArchiveRepository, WeeklyDigestArchiveRepository>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHttpClient<IOtpService, OtpService>();
        services.AddHttpClient<IPushNotificationService, FcmPushNotificationService>();

        // Generic GetDataBySearch / GetDataByCode infrastructure
        services.AddScoped<IStaticDataRepository, StaticDataRepository>();
        services.AddScoped<IStaticDataService, StaticDataService>();

        // Reusable BAL-internal GUID → INT PK resolver (wraps uspGetMasterDataByCodeInternal)
        services.AddScoped<IMasterDataResolver, MasterDataResolver>();

        // Foundation services — Phase A (Flow_Change_Analysis.md)
        services.AddSingleton<IApiLogService, ApiLogService>();   // Singleton: fire-and-forget, stateless
        services.AddScoped<IErrorCodeService, ErrorCodeService>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
