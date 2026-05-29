using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        builder.ToTable(
            "Rewards",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Rewards_Category",
                    "[Category] IN (N'ScreenTime', N'FoodTreat', N'Outing', N'Purchase', N'FamilyActivity')");
                table.HasCheckConstraint("CK_Rewards_CoinCost", "[CoinCost] BETWEEN 10 AND 9999");
                table.HasCheckConstraint("CK_Rewards_TimesRedeemedTotal", "[TimesRedeemedTotal] >= 0");
            });

        builder.HasKey(reward => reward.Id);

        builder.Property(reward => reward.Id).HasColumnName("RewardId").ValueGeneratedOnAdd();
        builder.Property(reward => reward.RewardName).HasMaxLength(200).IsRequired();
        builder.Property(reward => reward.Description).HasMaxLength(500);
        builder.Property(reward => reward.IconCode).HasMaxLength(50);
        builder.Property(reward => reward.Category).HasMaxLength(50).IsRequired();
        builder.Property(reward => reward.CoinCost).IsRequired();
        builder.Property(reward => reward.IsSystem).HasDefaultValue(false);
        builder.Property(reward => reward.IsEnabled).HasDefaultValue(true);
        builder.Property(reward => reward.TimesRedeemedTotal).HasDefaultValue(0);
        builder.Property(reward => reward.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(reward => reward.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(reward => new { reward.FamilyId, reward.IsEnabled })
            .HasDatabaseName("IX_Rewards_FamilyId_IsEnabled")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(reward => reward.Family)
            .WithMany()
            .HasForeignKey(reward => reward.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reward => reward.MasterReward)
            .WithMany()
            .HasForeignKey(reward => reward.MasterRewardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(reward => !reward.IsDeleted);
    }
}
