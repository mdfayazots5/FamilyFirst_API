using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RewardRedemptionConfiguration : IEntityTypeConfiguration<RewardRedemption>
{
    public void Configure(EntityTypeBuilder<RewardRedemption> builder)
    {
        builder.ConfigureBaseEntity("tblRewardRedemption", "RewardRedemptionId");

        builder.ToTable("tblRewardRedemption", table =>
        {
            table.HasCheckConstraint("CK_tblRewardRedemption_CoinsSpent", "[CoinsSpent] >= 0");
        });

        builder.Property(r => r.Status).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.RedemptionStatus.Pending);
        builder.Property(r => r.ParentNote).HasMaxLength(500);
        builder.Property(r => r.RequestedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(r => new { r.FamilyId, r.Status })
            .HasDatabaseName("IDX_tblRewardRedemption_FamilyId_Status")
            .HasFilter("[IsDeleted] = 0");

        // Idempotency index — prevents double redemptions
        builder.HasIndex(r => new { r.ChildProfileId, r.RewardId, r.Status })
            .IsUnique()
            .HasDatabaseName("UK_tblRewardRedemption_ChildProfileId_RewardId_Pending")
            .HasFilter("[IsDeleted] = 0 AND [Status] = 1");

        builder.HasOne(r => r.Reward)
            .WithMany()
            .HasForeignKey(r => r.RewardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ChildProfile)
            .WithMany()
            .HasForeignKey(r => r.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
