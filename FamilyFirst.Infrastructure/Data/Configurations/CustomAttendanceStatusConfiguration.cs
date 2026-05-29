using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CustomAttendanceStatusConfiguration : IEntityTypeConfiguration<CustomAttendanceStatus>
{
    public void Configure(EntityTypeBuilder<CustomAttendanceStatus> builder)
    {
        builder.ToTable("CustomAttendanceStatuses");
        builder.HasKey(status => status.StatusId);

        builder.Property(status => status.StatusId)
            .HasColumnName("StatusId")
            .ValueGeneratedOnAdd();

        builder.Property(status => status.StatusName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(status => status.ColorHex)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(status => status.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(status => new { status.FamilyId, status.StatusName })
            .IsUnique()
            .HasDatabaseName("UX_CustomAttendanceStatuses_FamilyId_StatusName");

        builder.HasOne(status => status.Family)
            .WithMany()
            .HasForeignKey(status => status.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
