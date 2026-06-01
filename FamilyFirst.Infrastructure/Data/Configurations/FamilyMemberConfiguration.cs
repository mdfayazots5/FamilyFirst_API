using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ConfigureBaseEntity("tblFamilyMember", "FamilyMemberId");

        builder.Property(m => m.Role).HasConversion<int>().IsRequired();
        builder.Property(m => m.LinkType).HasMaxLength(64).IsRequired();
        builder.Property(m => m.DisplayName).HasMaxLength(256);
        builder.Property(m => m.JoinedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(m => new { m.FamilyId, m.UserId })
            .IsUnique()
            .HasDatabaseName("UK_tblFamilyMember_FamilyId_UserId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(m => m.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(m => m.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InvitedByUser)
            .WithMany()
            .HasForeignKey(m => m.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
