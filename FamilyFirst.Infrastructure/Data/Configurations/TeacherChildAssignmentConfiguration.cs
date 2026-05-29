using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherChildAssignmentConfiguration : IEntityTypeConfiguration<TeacherChildAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherChildAssignment> builder)
    {
        builder.ToTable("TeacherChildAssignments");
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id).HasColumnName("AssignmentId").ValueGeneratedOnAdd();
        builder.Property(assignment => assignment.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(assignment => assignment.IsActive).HasDefaultValue(true);
        builder.Property(assignment => assignment.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(assignment => assignment.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(assignment => new { assignment.TeacherProfileId, assignment.ChildProfileId })
            .IsUnique()
            .HasDatabaseName("IX_TeacherChildAssignments_Teacher_Child")
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(assignment => assignment.FamilyId)
            .HasDatabaseName("IX_TeacherChildAssignments_FamilyId");

        builder.HasOne(assignment => assignment.TeacherProfile)
            .WithMany(profile => profile.ChildAssignments)
            .HasForeignKey(assignment => assignment.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.ChildProfile)
            .WithMany()
            .HasForeignKey(assignment => assignment.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Family)
            .WithMany()
            .HasForeignKey(assignment => assignment.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(assignment => !assignment.IsDeleted);
    }
}
