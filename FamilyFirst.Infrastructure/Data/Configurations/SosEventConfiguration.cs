using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class SosEventConfiguration : IEntityTypeConfiguration<SosEvent>
{
    public void Configure(EntityTypeBuilder<SosEvent> builder)
    {
        builder.ToTable("SOSEvents");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("SOSEventId").ValueGeneratedOnAdd();

        builder.Property(s => s.Latitude).HasPrecision(10, 7).IsRequired();
        builder.Property(s => s.Longitude).HasPrecision(10, 7).IsRequired();
        builder.Property(s => s.DispatchedAt).IsRequired();
        builder.Property(s => s.AlertsSentCount).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => new { s.FamilyId, s.ResolvedAt })
            .HasDatabaseName("IX_SOSEvents_FamilyId_ResolvedAt")
            .HasFilter("[IsDeleted] = 0 AND [ResolvedAt] IS NULL");

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ChildProfile)
            .WithMany()
            .HasForeignKey(s => s.ChildProfileId)
            .HasPrincipalKey("Id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.LocationAlert)
            .WithMany()
            .HasForeignKey(s => s.LocationAlertId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ResolvedByUser)
            .WithMany()
            .HasForeignKey(s => s.ResolvedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
