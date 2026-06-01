using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultDocumentVersionConfiguration : IEntityTypeConfiguration<VaultDocumentVersion>
{
    public void Configure(EntityTypeBuilder<VaultDocumentVersion> builder)
    {
        builder.ConfigureBaseEntity("tblVaultDocumentVersion", "VaultDocumentVersionId");

        builder.ToTable("tblVaultDocumentVersion", table =>
        {
            table.HasCheckConstraint("CK_tblVaultDocumentVersion_VersionNumber", "[VersionNumber] >= 1");
        });

        builder.Property(v => v.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.ArchivedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(v => v.VaultDocumentId)
            .HasDatabaseName("IDX_tblVaultDocumentVersion_VaultDocumentId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(v => v.FamilyId)
            .HasDatabaseName("IDX_tblVaultDocumentVersion_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(v => v.VaultDocument)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.VaultDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Family)
            .WithMany()
            .HasForeignKey(v => v.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.UploadedByUser)
            .WithMany()
            .HasForeignKey(v => v.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
