using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ConfigureBaseEntity("tblPlan", "PlanId");

        builder.Property(p => p.PlanName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.PlanCode).HasMaxLength(64).IsRequired();
        builder.Property(p => p.PriceMonthly).HasColumnType("money");

        builder.HasIndex(p => p.PlanName).IsUnique().HasDatabaseName("UK_tblPlan_PlanName");
        builder.HasIndex(p => p.PlanCode).IsUnique().HasDatabaseName("UK_tblPlan_PlanCode");
    }
}
