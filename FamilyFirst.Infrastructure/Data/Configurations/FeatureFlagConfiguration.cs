using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ConfigureBaseEntity("tblFeatureFlag", "FeatureFlagId");

        builder.Property(f => f.FlagKey).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FlagValue).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(300);

        builder.HasIndex(f => f.FlagKey)
            .IsUnique()
            .HasDatabaseName("UK_tblFeatureFlag_FlagKey")
            .HasFilter("[IsDeleted] = 0");
    }
}
