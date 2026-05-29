using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .HasColumnName("TokenId")
            .ValueGeneratedOnAdd();

        builder.Property(refreshToken => refreshToken.Token)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Ignore(refreshToken => refreshToken.UpdatedAt);
        builder.Ignore(refreshToken => refreshToken.IsDeleted);
        builder.Ignore(refreshToken => refreshToken.DeletedAt);

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_Token");

        builder.HasIndex(refreshToken => refreshToken.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
