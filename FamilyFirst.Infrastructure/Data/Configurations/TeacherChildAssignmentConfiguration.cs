using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherChildAssignmentConfiguration : IEntityTypeConfiguration<TeacherChildAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherChildAssignment> builder)
    {
        builder.ConfigureBaseEntity("tblTeacherChildAssignment", "TeacherChildAssignmentId");

        builder.Property(a => a.AssignedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(a => a.IsActive).HasDefaultValue(true);

        builder.HasIndex(a => new { a.TeacherProfileId, a.ChildProfileId })
            .IsUnique()
            .HasDatabaseName("UK_tblTeacherChildAssignment_TeacherProfileId_ChildProfileId")
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(a => a.FamilyId)
            .HasDatabaseName("IDX_tblTeacherChildAssignment_FamilyId");

        builder.HasOne(a => a.TeacherProfile)
            .WithMany(p => p.ChildAssignments)
            .HasForeignKey(a => a.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ChildProfile)
            .WithMany()
            .HasForeignKey(a => a.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
