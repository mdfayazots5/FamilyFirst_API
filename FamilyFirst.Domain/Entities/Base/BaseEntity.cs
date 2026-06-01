namespace FamilyFirst.Domain.Entities.Base;

public abstract class BaseEntity
{
    public long InternalId { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public int CompanyId { get; set; } = 1;

    public int SiteId { get; set; } = 1;

    public string CreatedBy { get; set; } = "Admin";

    public string IPAddress { get; set; } = "127.0.0.1";

    public DateTime DateCreated { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? LastUpdated { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DateDeleted { get; set; }

    public bool IsDeleted { get; set; }
}
