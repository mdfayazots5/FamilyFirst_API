using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RewardRedemptionConfiguration : IEntityTypeConfiguration<RewardRedemption>
{
    public void Configure(EntityTypeBuilder<RewardRedemption> builder)
    {
        builder.ToTable(
            "RewardRedemptions",
            table =>
            {
                table.HasCheckConstraint("CK_RewardRedemptions_CoinsSpent", "[CoinsSpent] >= 0");
            });

        builder.HasKey(redemption => redemption.Id);

        builder.Property(redemption => redemption.Id).HasColumnName("RedemptionId").ValueGeneratedOnAdd();
        builder.Property(redemption => redemption.Status).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.RedemptionStatus.Pending);
        builder.Property(redemption => redemption.ParentNote).HasMaxLength(500);
        builder.Property(redemption => redemption.RequestedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(redemption => redemption.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(redemption => redemption.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(redemption => new { redemption.FamilyId, redemption.Status })
            .HasDatabaseName("IX_RewardRedemptions_FamilyId_Status")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(redemption => new { redemption.ChildProfileId, redemption.RewardId, redemption.Status })
            .HasDatabaseName("UX_RewardRedemptions_ChildProfileId_RewardId_Pending")
            .HasFilter("[IsDeleted] = 0 AND [Status] = 1")
            .IsUnique();

        builder.HasOne(redemption => redemption.Reward)
            .WithMany()
            .HasForeignKey(redemption => redemption.RewardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(redemption => redemption.ChildProfile)
            .WithMany()
            .HasForeignKey(redemption => redemption.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(redemption => redemption.Family)
            .WithMany()
            .HasForeignKey(redemption => redemption.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(redemption => redemption.ReviewedByUser)
            .WithMany()
            .HasForeignKey(redemption => redemption.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(redemption => !redemption.IsDeleted);
    }
}
