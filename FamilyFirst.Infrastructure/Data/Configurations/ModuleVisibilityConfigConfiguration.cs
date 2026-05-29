using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class ModuleVisibilityConfigConfiguration : IEntityTypeConfiguration<ModuleVisibilityConfig>
{
    public void Configure(EntityTypeBuilder<ModuleVisibilityConfig> builder)
    {
        builder.ToTable("ModuleVisibilityConfig");
        builder.HasKey(config => config.ConfigId);

        builder.Property(config => config.ConfigId)
            .HasColumnName("ConfigId")
            .ValueGeneratedOnAdd();

        builder.Property(config => config.ModuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(config => config.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(config => new { config.FamilyId, config.RoleId, config.ModuleName })
            .IsUnique()
            .HasDatabaseName("UX_ModuleVisibilityConfig_FamilyId_RoleId_ModuleName");

        builder.HasOne(config => config.Family)
            .WithMany()
            .HasForeignKey(config => config.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
