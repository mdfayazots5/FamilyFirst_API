using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");
        builder.HasKey(featureFlag => featureFlag.FlagKey);

        builder.Property(featureFlag => featureFlag.FlagKey)
            .HasColumnName("FlagKey")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(featureFlag => featureFlag.FlagValue)
            .HasColumnName("FlagValue")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(featureFlag => featureFlag.Description)
            .HasColumnName("Description")
            .HasMaxLength(300);

        builder.Property(featureFlag => featureFlag.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
