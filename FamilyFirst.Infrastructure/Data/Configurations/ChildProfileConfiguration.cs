using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable(
            "ChildProfiles",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_ChildProfiles_AvatarCode",
                    "[AvatarCode] IN (N'avatar_01', N'avatar_02', N'avatar_03', N'avatar_04', N'avatar_05', N'avatar_06', N'avatar_07', N'avatar_08', N'avatar_09', N'avatar_10')");
                table.HasCheckConstraint("CK_ChildProfiles_CoinBalance", "[CoinBalance] >= 0");
                table.HasCheckConstraint("CK_ChildProfiles_TotalCoinsEarned", "[TotalCoinsEarned] >= 0");
                table.HasCheckConstraint("CK_ChildProfiles_StreakFreezesAvailable", "[StreakFreezesAvailable] BETWEEN 0 AND 2");
                table.HasCheckConstraint("CK_ChildProfiles_LevelCode", "[LevelCode] >= 1");
                table.HasCheckConstraint("CK_ChildProfiles_StudyScore", "[StudyScore] BETWEEN 0 AND 20");
                table.HasCheckConstraint("CK_ChildProfiles_CleanlinessScore", "[CleanlinessScore] BETWEEN 0 AND 20");
                table.HasCheckConstraint("CK_ChildProfiles_DisciplineScore", "[DisciplineScore] BETWEEN 0 AND 20");
                table.HasCheckConstraint("CK_ChildProfiles_ScreenControlScore", "[ScreenControlScore] BETWEEN 0 AND 20");
                table.HasCheckConstraint("CK_ChildProfiles_ResponsibilityScore", "[ResponsibilityScore] BETWEEN 0 AND 20");
            });

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id).HasColumnName("ChildProfileId").ValueGeneratedOnAdd();
        builder.Property(profile => profile.DateOfBirth).HasColumnType("date");
        builder.Property(profile => profile.GradeLevel).HasMaxLength(50);
        builder.Property(profile => profile.SchoolName).HasMaxLength(200);
        builder.Property(profile => profile.AvatarCode).HasMaxLength(20).IsRequired().HasDefaultValue("avatar_01");
        builder.Property(profile => profile.CoinBalance).HasDefaultValue(0);
        builder.Property(profile => profile.TotalCoinsEarned).HasDefaultValue(0);
        builder.Property(profile => profile.CurrentStreakDays).HasDefaultValue(0);
        builder.Property(profile => profile.BestStreakDays).HasDefaultValue(0);
        builder.Property(profile => profile.StreakFreezesAvailable).HasDefaultValue(0);
        builder.Property(profile => profile.LevelCode).HasDefaultValue(1);
        builder.Property(profile => profile.StudyScore).HasDefaultValue(0);
        builder.Property(profile => profile.CleanlinessScore).HasDefaultValue(0);
        builder.Property(profile => profile.DisciplineScore).HasDefaultValue(0);
        builder.Property(profile => profile.ScreenControlScore).HasDefaultValue(0);
        builder.Property(profile => profile.ResponsibilityScore).HasDefaultValue(0);
        builder.Property(profile => profile.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(profile => profile.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(profile => profile.RowVersion).IsRowVersion();

        builder.HasIndex(profile => profile.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UX_ChildProfiles_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(profile => profile.FamilyId)
            .HasDatabaseName("IX_ChildProfiles_FamilyId");

        builder.HasOne(profile => profile.FamilyMember)
            .WithOne()
            .HasForeignKey<ChildProfile>(profile => profile.FamilyMemberId)
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
