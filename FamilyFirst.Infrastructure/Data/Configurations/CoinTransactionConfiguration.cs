using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CoinTransactionConfiguration : IEntityTypeConfiguration<CoinTransaction>
{
    public void Configure(EntityTypeBuilder<CoinTransaction> builder)
    {
        builder.ToTable("CoinTransactions");
        builder.HasKey(transaction => transaction.TransactionId);

        builder.Property(transaction => transaction.TransactionId).ValueGeneratedOnAdd();
        builder.Property(transaction => transaction.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(transaction => transaction.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(transaction => transaction.Note).HasMaxLength(500);
        builder.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(transaction => new { transaction.ChildProfileId, transaction.CreatedAt })
            .HasDatabaseName("IX_CoinTransactions_ChildProfileId_CreatedAt");

        builder.HasOne(transaction => transaction.ChildProfile)
            .WithMany()
            .HasForeignKey(transaction => transaction.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Family)
            .WithMany()
            .HasForeignKey(transaction => transaction.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.CreatedByUser)
            .WithMany()
            .HasForeignKey(transaction => transaction.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
