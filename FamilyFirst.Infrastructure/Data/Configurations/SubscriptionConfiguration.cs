using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ConfigureBaseEntity("tblSubscription", "SubscriptionId");

        builder.Property(s => s.Status).HasMaxLength(24).IsRequired();
        builder.Property(s => s.RazorpaySubscriptionId).HasMaxLength(256);
        builder.Property(s => s.RazorpayCustomerId).HasMaxLength(256);

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
