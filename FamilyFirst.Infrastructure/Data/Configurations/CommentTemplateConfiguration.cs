using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CommentTemplateConfiguration : IEntityTypeConfiguration<CommentTemplate>
{
    public void Configure(EntityTypeBuilder<CommentTemplate> builder)
    {
        builder.ToTable("CommentTemplates");
        builder.HasKey(template => template.TemplateId);

        builder.Property(template => template.TemplateId)
            .ValueGeneratedOnAdd();

        builder.Property(template => template.TemplateText)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(template => template.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(template => template.IsSystem)
            .HasDefaultValue(false);

        builder.Property(template => template.IsActive)
            .HasDefaultValue(true);

        builder.Property(template => template.SortOrder)
            .HasDefaultValue(0);

        builder.Property(template => template.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(template => new { template.FamilyId, template.Category, template.IsActive })
            .HasDatabaseName("IX_CommentTemplates_FamilyId_Category");

        builder.HasOne(template => template.Family)
            .WithMany()
            .HasForeignKey(template => template.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(template => template.IsActive);
    }
}
