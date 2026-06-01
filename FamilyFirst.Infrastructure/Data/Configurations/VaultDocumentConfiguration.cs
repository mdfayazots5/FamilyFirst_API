using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultDocumentConfiguration : IEntityTypeConfiguration<VaultDocument>
{
    public void Configure(EntityTypeBuilder<VaultDocument> builder)
    {
        builder.ConfigureBaseEntity("tblVaultDocument", "VaultDocumentId");

        builder.ToTable("tblVaultDocument", table =>
        {
            table.HasCheckConstraint("CK_tblVaultDocument_Category",    "[Category]    BETWEEN 1 AND 8");
            table.HasCheckConstraint("CK_tblVaultDocument_Visibility",  "[Visibility]  BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_tblVaultDocument_VersionNumber", "[VersionNumber] >= 1");
        });

        builder.Property(d => d.DocumentName).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Category).HasConversion<int>().IsRequired();
        builder.Property(d => d.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Tags).HasMaxLength(2000);
        builder.Property(d => d.Visibility).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.DocumentVisibility.ParentsOnly);
        builder.Property(d => d.VersionNumber).IsRequired().HasDefaultValue(1);
        builder.Property(d => d.IsCurrentVersion).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.IsEmergencyPriority).IsRequired().HasDefaultValue(false);

        builder.HasIndex(d => d.FamilyId)
            .HasDatabaseName("IDX_tblVaultDocument_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(d => new { d.FamilyId, d.ExpiryDate })
            .HasDatabaseName("IDX_tblVaultDocument_FamilyId_ExpiryDate")
            .HasFilter("[IsDeleted] = 0 AND [IsCurrentVersion] = 1 AND [ExpiryDate] IS NOT NULL");

        builder.HasIndex(d => d.FamilyMemberId)
            .HasDatabaseName("IDX_tblVaultDocument_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(d => new { d.FamilyId, d.IsEmergencyPriority })
            .HasDatabaseName("IDX_tblVaultDocument_FamilyId_IsEmergencyPriority")
            .HasFilter("[IsDeleted] = 0 AND [IsCurrentVersion] = 1 AND [IsEmergencyPriority] = 1");

        builder.HasOne(d => d.Family)
            .WithMany()
            .HasForeignKey(d => d.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.FamilyMember)
            .WithMany()
            .HasForeignKey(d => d.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UploadedByUser)
            .WithMany()
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Versions)
            .WithOne(v => v.VaultDocument)
            .HasForeignKey(v => v.VaultDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.ShareLinks)
            .WithOne(s => s.VaultDocument)
            .HasForeignKey(s => s.VaultDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
