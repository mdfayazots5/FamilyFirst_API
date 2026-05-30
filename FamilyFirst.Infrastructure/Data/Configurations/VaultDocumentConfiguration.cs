using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultDocumentConfiguration : IEntityTypeConfiguration<VaultDocument>
{
    public void Configure(EntityTypeBuilder<VaultDocument> builder)
    {
        builder.ToTable(
            "VaultDocuments",
            table =>
            {
                table.HasCheckConstraint("CK_VaultDocuments_Category",    "[Category]    BETWEEN 1 AND 8");
                table.HasCheckConstraint("CK_VaultDocuments_Visibility",  "[Visibility]  BETWEEN 1 AND 4");
                table.HasCheckConstraint("CK_VaultDocuments_VersionNumber", "[VersionNumber] >= 1");
            });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("DocumentId").ValueGeneratedOnAdd();

        builder.Property(d => d.DocumentName).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Category).HasConversion<int>().IsRequired();
        builder.Property(d => d.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Tags).HasMaxLength(2000);
        builder.Property(d => d.Visibility).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.DocumentVisibility.ParentsOnly);
        builder.Property(d => d.VersionNumber).IsRequired().HasDefaultValue(1);
        builder.Property(d => d.IsCurrentVersion).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.IsEmergencyPriority).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(d => new { d.FamilyId, d.IsDeleted })
            .HasDatabaseName("IX_VaultDocuments_FamilyId_IsDeleted")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(d => new { d.FamilyId, d.ExpiryDate })
            .HasDatabaseName("IX_VaultDocuments_FamilyId_ExpiryDate")
            .HasFilter("[IsDeleted] = 0 AND [IsCurrentVersion] = 1 AND [ExpiryDate] IS NOT NULL");

        builder.HasIndex(d => d.MemberId)
            .HasDatabaseName("IX_VaultDocuments_MemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(d => new { d.FamilyId, d.IsEmergencyPriority })
            .HasDatabaseName("IX_VaultDocuments_FamilyId_IsEmergencyPriority")
            .HasFilter("[IsDeleted] = 0 AND [IsCurrentVersion] = 1 AND [IsEmergencyPriority] = 1");

        builder.HasOne(d => d.Family)
            .WithMany()
            .HasForeignKey(d => d.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Member)
            .WithMany()
            .HasForeignKey(d => d.MemberId)
            .HasPrincipalKey("Id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UploadedByUser)
            .WithMany()
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Versions)
            .WithOne(v => v.Document)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.ShareLinks)
            .WithOne(s => s.Document)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
