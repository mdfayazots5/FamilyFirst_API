using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultShareLinkConfiguration : IEntityTypeConfiguration<VaultShareLink>
{
    public void Configure(EntityTypeBuilder<VaultShareLink> builder)
    {
        builder.ConfigureBaseEntity("tblVaultShareLink", "VaultShareLinkId");

        builder.Property(s => s.Token).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.AllowDownload).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.IsRevoked).IsRequired().HasDefaultValue(false);

        builder.HasIndex(s => s.Token)
            .IsUnique()
            .HasDatabaseName("UK_tblVaultShareLink_Token")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => new { s.VaultDocumentId, s.IsRevoked })
            .HasDatabaseName("IDX_tblVaultShareLink_VaultDocumentId_IsRevoked")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(s => s.VaultDocument)
            .WithMany(d => d.ShareLinks)
            .HasForeignKey(s => s.VaultDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
