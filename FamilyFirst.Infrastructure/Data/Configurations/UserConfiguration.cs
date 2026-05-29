using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("UserId")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.CountryCode)
            .HasMaxLength(5)
            .IsRequired()
            .HasDefaultValue("+91");

        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(300);

        builder.Property(user => user.ProfilePhotoUrl)
            .HasMaxLength(500);

        builder.Property(user => user.PinHash)
            .HasMaxLength(500);

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500);

        builder.Property(user => user.FcmToken)
            .HasMaxLength(500);

        builder.Property(user => user.PreferredLanguage)
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("en");

        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(user => user.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("UX_Users_PhoneNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email")
            .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasQueryFilter(user => !user.IsDeleted);
    }
}
