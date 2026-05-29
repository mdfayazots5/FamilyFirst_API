using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(plan => plan.PlanId);

        builder.Property(plan => plan.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(plan => plan.PlanCode).HasMaxLength(50).IsRequired();
        builder.Property(plan => plan.PriceMonthly).HasColumnType("decimal(10,2)");

        builder.HasIndex(plan => plan.PlanName).IsUnique().HasDatabaseName("UX_Plans_PlanName");
        builder.HasIndex(plan => plan.PlanCode).IsUnique().HasDatabaseName("UX_Plans_PlanCode");
    }
}
