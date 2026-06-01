using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> builder)
    {
        builder.ConfigureBaseEntity("tblTeacherProfile", "TeacherProfileId");

        builder.ToTable("tblTeacherProfile", table =>
        {
            table.HasCheckConstraint("CK_tblTeacherProfile_TeacherType",
                "[TeacherType] IN (N'School', N'Tuition', N'Arabic', N'Music', N'Other')");
        });

        builder.Property(p => p.SubjectName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.TeacherType).HasMaxLength(64).IsRequired();
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UK_tblTeacherProfile_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.FamilyId)
            .HasDatabaseName("IDX_tblTeacherProfile_FamilyId");

        builder.HasOne(p => p.FamilyMember)
            .WithOne()
            .HasForeignKey<TeacherProfile>(p => p.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
