using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultDocumentVersionConfiguration : IEntityTypeConfiguration<VaultDocumentVersion>
{
    public void Configure(EntityTypeBuilder<VaultDocumentVersion> builder)
    {
        builder.ToTable(
            "VaultDocumentVersions",
            table =>
            {
                table.HasCheckConstraint("CK_VaultDocumentVersions_VersionNumber", "[VersionNumber] >= 1");
            });

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("VersionId").ValueGeneratedOnAdd();

        builder.Property(v => v.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.ArchivedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(v => new { v.DocumentId, v.VersionNumber })
            .HasDatabaseName("IX_VaultDocumentVersions_DocumentId")
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(v => new { v.FamilyId, v.IsDeleted })
            .HasDatabaseName("IX_VaultDocumentVersions_FamilyId_IsDeleted")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(v => v.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Family)
            .WithMany()
            .HasForeignKey(v => v.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.UploadedByUser)
            .WithMany()
            .HasForeignKey(v => v.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
