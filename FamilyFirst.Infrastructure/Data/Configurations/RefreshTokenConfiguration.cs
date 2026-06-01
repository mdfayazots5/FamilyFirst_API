using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ConfigureBaseEntity("tblRefreshToken", "RefreshTokenId");

        builder.Property(rt => rt.Token).HasMaxLength(512).IsRequired();
        builder.Property(rt => rt.DeviceInfo).HasMaxLength(512);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("UK_tblRefreshToken_Token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IDX_tblRefreshToken_UserId");

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
