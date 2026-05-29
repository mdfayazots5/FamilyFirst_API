using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> builder)
    {
        builder.ToTable(
            "TeacherProfiles",
            table =>
            {
                table.HasCheckConstraint("CK_TeacherProfiles_TeacherType", "[TeacherType] IN (N'School', N'Tuition', N'Arabic', N'Music', N'Other')");
            });

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id).HasColumnName("TeacherProfileId").ValueGeneratedOnAdd();
        builder.Property(profile => profile.SubjectName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.TeacherType).HasMaxLength(50).IsRequired();
        builder.Property(profile => profile.IsActive).HasDefaultValue(true);
        builder.Property(profile => profile.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(profile => profile.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(profile => profile.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UX_TeacherProfiles_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(profile => profile.FamilyId)
            .HasDatabaseName("IX_TeacherProfiles_FamilyId");

        builder.HasOne(profile => profile.FamilyMember)
            .WithOne()
            .HasForeignKey<TeacherProfile>(profile => profile.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.User)
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.Family)
            .WithMany()
            .HasForeignKey(profile => profile.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(profile => !profile.IsDeleted);
    }
}
