using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data;

public sealed class FamilyFirstDbContext : DbContext
{
    public FamilyFirstDbContext(DbContextOptions<FamilyFirstDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<TeacherChildAssignment> TeacherChildAssignments => Set<TeacherChildAssignment>();

    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<CommentTemplate> CommentTemplates => Set<CommentTemplate>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<TaskCompletion> TaskCompletions => Set<TaskCompletion>();

    public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();

    public DbSet<TeacherFeedback> TeacherFeedback => Set<TeacherFeedback>();

    public DbSet<Reward> Rewards => Set<Reward>();

    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<EventReminder> EventReminders => Set<EventReminder>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<ModuleVisibilityConfig> ModuleVisibilityConfigs => Set<ModuleVisibilityConfig>();

    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();

    public DbSet<CustomAttendanceStatus> CustomAttendanceStatuses => Set<CustomAttendanceStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamilyFirstDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateBaseEntityTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateBaseEntityTimestamps();
        return base.SaveChanges();
    }

    private void UpdateBaseEntityTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
