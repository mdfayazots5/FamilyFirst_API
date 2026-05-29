using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("FamilyMembers");
        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id).HasColumnName("FamilyMemberId").ValueGeneratedOnAdd();
        builder.Property(member => member.Role).HasConversion<int>().IsRequired();
        builder.Property(member => member.LinkType).HasMaxLength(50).IsRequired();
        builder.Property(member => member.DisplayName).HasMaxLength(200);
        builder.Property(member => member.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(member => member.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(member => member.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(member => new { member.FamilyId, member.UserId })
            .IsUnique()
            .HasDatabaseName("IX_FamilyMembers_FamilyId_UserId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(member => member.Family)
            .WithMany(family => family.Members)
            .HasForeignKey(member => member.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(member => member.User)
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(member => member.InvitedByUser)
            .WithMany()
            .HasForeignKey(member => member.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(member => !member.IsDeleted);
    }
}
