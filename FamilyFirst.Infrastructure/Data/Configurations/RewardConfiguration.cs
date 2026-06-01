using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        builder.ConfigureBaseEntity("tblReward", "RewardId");

        builder.ToTable("tblReward", table =>
        {
            table.HasCheckConstraint(
                "CK_tblReward_Category",
                "[Category] IN (N'ScreenTime', N'FoodTreat', N'Outing', N'Purchase', N'FamilyActivity')");
            table.HasCheckConstraint("CK_tblReward_CoinCost", "[CoinCost] BETWEEN 10 AND 9999");
            table.HasCheckConstraint("CK_tblReward_TimesRedeemedTotal", "[TimesRedeemedTotal] >= 0");
        });

        builder.Property(r => r.RewardName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.IconCode).HasMaxLength(50);
        builder.Property(r => r.Category).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CoinCost).IsRequired();
        builder.Property(r => r.IsSystem).HasDefaultValue(false);
        builder.Property(r => r.IsEnabled).HasDefaultValue(true);
        builder.Property(r => r.TimesRedeemedTotal).HasDefaultValue(0);

        builder.HasIndex(r => new { r.FamilyId, r.IsEnabled })
            .HasDatabaseName("IDX_tblReward_FamilyId_IsEnabled")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.MasterReward)
            .WithMany()
            .HasForeignKey(r => r.MasterRewardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
