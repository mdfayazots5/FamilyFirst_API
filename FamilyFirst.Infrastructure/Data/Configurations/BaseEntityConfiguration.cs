using FamilyFirst.Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Applies the standard New SQL Format mapping for all BaseEntity-derived tables:
    /// BIGINT IDENTITY PK + GUID Id + audit columns + IsDeleted query filter.
    /// </summary>
    public static void ConfigureBaseEntity<T>(
        this EntityTypeBuilder<T> builder,
        string tableName,
        string internalIdColumnName)
        where T : BaseEntity
    {
        builder.ToTable(tableName);

        builder.HasKey(e => e.InternalId);
        builder.Property(e => e.InternalId)
            .HasColumnName(internalIdColumnName)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(e => e.CompanyId)
            .HasDefaultValue(1);

        builder.Property(e => e.SiteId)
            .HasDefaultValue(1);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(128)
            .HasDefaultValue("Admin");

        builder.Property(e => e.IPAddress)
            .HasMaxLength(64)
            .HasDefaultValue("127.0.0.1");

        builder.Property(e => e.DateCreated)
            .HasColumnName("DateCreated")
            .HasDefaultValueSql("GETDATE()");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasMaxLength(128);

        builder.Property(e => e.LastUpdated)
            .HasColumnName("LastUpdated");

        builder.Property(e => e.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasMaxLength(128);

        builder.Property(e => e.DateDeleted)
            .HasColumnName("DateDeleted");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Applies the standard New SQL Format mapping for AppendOnlyEntity-derived tables.
    /// No IsDeleted, no UpdatedBy/LastUpdated/DeletedBy.
    /// </summary>
    public static void ConfigureAppendOnlyEntity<T>(
        this EntityTypeBuilder<T> builder,
        string tableName,
        string internalIdColumnName)
        where T : AppendOnlyEntity
    {
        builder.ToTable(tableName);

        builder.HasKey(e => e.InternalId);
        builder.Property(e => e.InternalId)
            .HasColumnName(internalIdColumnName)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(e => e.CompanyId)
            .HasDefaultValue(1);

        builder.Property(e => e.SiteId)
            .HasDefaultValue(1);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(128)
            .HasDefaultValue("Admin");

        builder.Property(e => e.IPAddress)
            .HasMaxLength(64)
            .HasDefaultValue("127.0.0.1");

        builder.Property(e => e.DateCreated)
            .HasColumnName("DateCreated")
            .HasDefaultValueSql("GETDATE()");
    }
}
