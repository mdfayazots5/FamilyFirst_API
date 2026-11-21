using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class ReportExportConfiguration : IEntityTypeConfiguration<ReportExport>
{
    public void Configure(EntityTypeBuilder<ReportExport> builder)
    {
        builder.ToTable("tblReportExport");
        builder.HasKey(export => export.InternalId);
        builder.Property(export => export.InternalId).HasColumnName("ReportExportId").ValueGeneratedOnAdd();
        builder.Property(export => export.Id).HasDefaultValueSql("NEWID()");
        builder.Property(export => export.ReportType).HasMaxLength(32);
        builder.Property(export => export.Period).HasMaxLength(16);
        builder.Property(export => export.Format).HasMaxLength(16);
        builder.Property(export => export.Status).HasMaxLength(24);
        builder.Property(export => export.DownloadUrl).HasMaxLength(4000);
        builder.Property(export => export.ErrorMessage).HasMaxLength(512);
        builder.Ignore(export => export.CreatedAt);
        builder.Ignore(export => export.UpdatedAt);
        builder.Ignore(export => export.DeletedAt);
        builder.HasQueryFilter(export => !export.IsDeleted);

        builder.HasOne<Family>().WithMany().HasForeignKey(export => export.FamilyId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(export => export.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ChildProfile>().WithMany().HasForeignKey(export => export.ChildProfileId).OnDelete(DeleteBehavior.NoAction);
    }
}
