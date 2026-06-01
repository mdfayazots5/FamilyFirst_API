using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ConfigureBaseEntity("tblFamily", "FamilyId");

        builder.Property(f => f.FamilyName).HasMaxLength(256).IsRequired();
        builder.Property(f => f.JoinCode).HasMaxLength(16).IsRequired();
        builder.Property(f => f.City).HasMaxLength(128);
        builder.Property(f => f.TimezoneId).HasMaxLength(128).IsRequired().HasDefaultValue("Asia/Kolkata");

        builder.HasIndex(f => f.JoinCode)
            .IsUnique()
            .HasDatabaseName("UK_tblFamily_JoinCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(f => f.Plan)
            .WithMany()
            .HasForeignKey(f => f.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.FamilyAdminUser)
            .WithMany()
            .HasForeignKey(f => f.FamilyAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(f => f.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(f => f.Subscription);
    }
}
