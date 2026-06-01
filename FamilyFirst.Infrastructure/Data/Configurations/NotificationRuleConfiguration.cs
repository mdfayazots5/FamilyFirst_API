using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ConfigureBaseEntity("tblNotificationRule", "NotificationRuleId");

        builder.Property(r => r.RuleKey).HasMaxLength(50).IsRequired();
        builder.Property(r => r.PriorityOverride).HasConversion<int?>();

        builder.HasIndex(r => new { r.FamilyId, r.RuleKey })
            .IsUnique()
            .HasDatabaseName("UK_tblNotificationRule_FamilyId_RuleKey")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
