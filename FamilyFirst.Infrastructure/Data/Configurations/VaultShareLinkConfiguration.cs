using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultShareLinkConfiguration : IEntityTypeConfiguration<VaultShareLink>
{
    public void Configure(EntityTypeBuilder<VaultShareLink> builder)
    {
        builder.ToTable("VaultShareLinks");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("ShareLinkId").ValueGeneratedOnAdd();

        builder.Property(s => s.Token).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.AllowDownload).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => s.Token)
            .HasDatabaseName("IX_VaultShareLinks_Token")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => new { s.DocumentId, s.IsRevoked })
            .HasDatabaseName("IX_VaultShareLinks_DocumentId_IsRevoked")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(s => s.Document)
            .WithMany(d => d.ShareLinks)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
