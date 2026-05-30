using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class SafeZone : BaseEntity
{
    public Guid FamilyId { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public string ZoneType { get; set; } = string.Empty;

    public decimal CenterLatitude { get; set; }

    public decimal CenterLongitude { get; set; }

    public int RadiusMetres { get; set; } = 150;

    public bool AlertOnArrival { get; set; } = true;

    public bool AlertOnDeparture { get; set; } = true;

    public bool LateAlertEnabled { get; set; }

    public TimeOnly? LateAlertTime { get; set; }

    public bool OverrideQuietHours { get; set; } = true;

    public string AppliedMemberIdsJson { get; set; } = "[]";

    public Family Family { get; set; } = null!;
}
