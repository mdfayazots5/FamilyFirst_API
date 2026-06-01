using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CoinTransactionConfiguration : IEntityTypeConfiguration<CoinTransaction>
{
    public void Configure(EntityTypeBuilder<CoinTransaction> builder)
    {
        builder.ConfigureAppendOnlyEntity("tblCoinTransaction", "CoinTransactionId");

        builder.Property(t => t.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(t => t.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Note).HasMaxLength(500);

        builder.HasIndex(t => new { t.ChildProfileId, t.DateCreated })
            .HasDatabaseName("IDX_tblCoinTransaction_ChildProfileId_DateCreated");

        builder.HasOne(t => t.ChildProfile)
            .WithMany()
            .HasForeignKey(t => t.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
