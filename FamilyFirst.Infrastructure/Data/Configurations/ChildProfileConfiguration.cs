using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ConfigureBaseEntity("tblChildProfile", "ChildProfileId");

        builder.ToTable("tblChildProfile", table =>
        {
            table.HasCheckConstraint("CK_tblChildProfile_AvatarCode",
                "[AvatarCode] IN (N'avatar_01', N'avatar_02', N'avatar_03', N'avatar_04', N'avatar_05', N'avatar_06', N'avatar_07', N'avatar_08', N'avatar_09', N'avatar_10')");
            table.HasCheckConstraint("CK_tblChildProfile_CoinBalance", "[CoinBalance] >= 0");
            table.HasCheckConstraint("CK_tblChildProfile_TotalCoinsEarned", "[TotalCoinsEarned] >= 0");
            table.HasCheckConstraint("CK_tblChildProfile_StreakFreezesAvailable", "[StreakFreezesAvailable] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_tblChildProfile_LevelCode", "[LevelCode] >= 1");
            table.HasCheckConstraint("CK_tblChildProfile_StudyScore", "[StudyScore] BETWEEN 0 AND 20");
            table.HasCheckConstraint("CK_tblChildProfile_CleanlinessScore", "[CleanlinessScore] BETWEEN 0 AND 20");
            table.HasCheckConstraint("CK_tblChildProfile_DisciplineScore", "[DisciplineScore] BETWEEN 0 AND 20");
            table.HasCheckConstraint("CK_tblChildProfile_ScreenControlScore", "[ScreenControlScore] BETWEEN 0 AND 20");
            table.HasCheckConstraint("CK_tblChildProfile_ResponsibilityScore", "[ResponsibilityScore] BETWEEN 0 AND 20");
        });

        builder.Property(p => p.GradeLevel).HasMaxLength(64);
        builder.Property(p => p.SchoolName).HasMaxLength(256);
        builder.Property(p => p.AvatarCode).HasMaxLength(24).IsRequired().HasDefaultValue("avatar_01");
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UK_tblChildProfile_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.FamilyId)
            .HasDatabaseName("IDX_tblChildProfile_FamilyId");

        builder.HasOne(p => p.FamilyMember)
            .WithOne()
            .HasForeignKey<ChildProfile>(p => p.FamilyMemberId)
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
