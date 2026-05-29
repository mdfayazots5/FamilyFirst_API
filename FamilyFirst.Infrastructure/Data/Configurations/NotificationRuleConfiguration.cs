using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("NotificationRules");
        builder.HasKey(rule => rule.RuleId);

        builder.Property(rule => rule.RuleId)
            .HasColumnName("RuleId")
            .ValueGeneratedOnAdd();

        builder.Property(rule => rule.RuleKey)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(rule => rule.PriorityOverride)
            .HasConversion<int?>();

        builder.Property(rule => rule.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(rule => new { rule.FamilyId, rule.RuleKey })
            .IsUnique()
            .HasDatabaseName("UX_NotificationRules_FamilyId_RuleKey");

        builder.HasOne(rule => rule.Family)
            .WithMany()
            .HasForeignKey(rule => rule.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
