using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("Families");
        builder.HasKey(family => family.Id);

        builder.Property(family => family.Id).HasColumnName("FamilyId").ValueGeneratedOnAdd();
        builder.Property(family => family.FamilyName).HasMaxLength(200).IsRequired();
        builder.Property(family => family.JoinCode).HasMaxLength(10).IsRequired();
        builder.Property(family => family.City).HasMaxLength(100);
        builder.Property(family => family.TimezoneId).HasMaxLength(100).IsRequired().HasDefaultValue("Asia/Kolkata");
        builder.Property(family => family.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(family => family.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(family => family.JoinCode)
            .IsUnique()
            .HasDatabaseName("UX_Families_JoinCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(family => family.Plan)
            .WithMany()
            .HasForeignKey(family => family.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(family => family.FamilyAdminUser)
            .WithMany()
            .HasForeignKey(family => family.FamilyAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(family => family.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(family => family.Subscription);
        builder.HasQueryFilter(family => !family.IsDeleted);
    }
}
