using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CommentTemplateConfiguration : IEntityTypeConfiguration<CommentTemplate>
{
    public void Configure(EntityTypeBuilder<CommentTemplate> builder)
    {
        builder.ConfigureBaseEntity("tblCommentTemplate", "CommentTemplateId");

        builder.Property(t => t.TemplateText).HasMaxLength(512).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(64).IsRequired();
        builder.Property(t => t.IsSystem).HasDefaultValue(false);
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.SortOrder).HasDefaultValue(0);

        builder.HasIndex(t => new { t.FamilyId, t.Category, t.IsActive })
            .HasDatabaseName("IDX_tblCommentTemplate_FamilyId_Category");

        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
