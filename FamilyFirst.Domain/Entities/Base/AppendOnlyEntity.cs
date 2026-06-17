namespace FamilyFirst.Domain.Entities.Base;

/// <summary>
/// Base for append-only tables: tblCoinTransaction, tblAuditLog, tblLocationHistory, tblChildPillarScoreHistory.
/// No IsDeleted, no UpdatedBy/LastUpdated/DeletedBy columns — records are never modified or soft-deleted.
/// </summary>
public abstract class AppendOnlyEntity
{
    public long InternalId { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public int CompanyId { get; set; } = 1;

    public int SiteId { get; set; } = 1;

    public string IPAddress { get; set; } = "127.0.0.1";

    public string CreatedBy { get; set; } = "Admin";

    public DateTime DateCreated { get; set; }

    public DateTime CreatedAt
    {
        get => DateCreated;
        set => DateCreated = value;
    }
}
