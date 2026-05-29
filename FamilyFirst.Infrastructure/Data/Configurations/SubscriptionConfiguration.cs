using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Id).HasColumnName("SubscriptionId").ValueGeneratedOnAdd();
        builder.Property(subscription => subscription.Status).HasMaxLength(20).IsRequired();
        builder.Property(subscription => subscription.RazorpaySubscriptionId).HasMaxLength(200);
        builder.Property(subscription => subscription.RazorpayCustomerId).HasMaxLength(200);
        builder.Property(subscription => subscription.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(subscription => subscription.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Ignore(subscription => subscription.IsDeleted);
        builder.Ignore(subscription => subscription.DeletedAt);

        builder.HasOne(subscription => subscription.Family)
            .WithMany()
            .HasForeignKey(subscription => subscription.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(subscription => subscription.Plan)
            .WithMany()
            .HasForeignKey(subscription => subscription.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
