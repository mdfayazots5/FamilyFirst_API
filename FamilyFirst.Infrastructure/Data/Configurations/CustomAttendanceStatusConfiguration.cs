using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CustomAttendanceStatusConfiguration : IEntityTypeConfiguration<CustomAttendanceStatus>
{
    public void Configure(EntityTypeBuilder<CustomAttendanceStatus> builder)
    {
        builder.ConfigureBaseEntity("tblCustomAttendanceStatus", "CustomAttendanceStatusId");

        builder.Property(s => s.StatusName).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ColorHex).HasMaxLength(7).IsRequired();

        builder.HasIndex(s => new { s.FamilyId, s.StatusName })
            .IsUnique()
            .HasDatabaseName("UK_tblCustomAttendanceStatus_FamilyId_StatusName")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
