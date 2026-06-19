using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureBaseEntity("tblUser", "UserId");

        builder.Property(u => u.PhoneNumber).HasMaxLength(24).IsRequired();
        builder.Property(u => u.CountryCode).HasMaxLength(8).IsRequired().HasDefaultValue("+91");
        builder.Property(u => u.FullName).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(512);
        builder.Property(u => u.ProfilePhotoUrl).HasMaxLength(512);
        builder.Property(u => u.PinHash).HasMaxLength(512);
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.FcmToken).HasMaxLength(512);
        builder.Property(u => u.IsDefaultPassword).HasDefaultValue(false);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(16).IsRequired().HasDefaultValue("en");

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("UK_tblUser_PhoneNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UK_tblUser_Email")
            .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");
    }
}
