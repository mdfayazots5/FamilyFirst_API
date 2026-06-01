using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class ModuleVisibilityConfigConfiguration : IEntityTypeConfiguration<ModuleVisibilityConfig>
{
    public void Configure(EntityTypeBuilder<ModuleVisibilityConfig> builder)
    {
        builder.ConfigureBaseEntity("tblModuleVisibilityConfig", "ModuleVisibilityConfigId");

        builder.Property(c => c.ModuleName).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => new { c.FamilyId, c.RoleId, c.ModuleName })
            .IsUnique()
            .HasDatabaseName("UK_tblModuleVisibilityConfig_FamilyId_RoleId_ModuleName")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
