namespace FamilyFirst.Domain.Enums;

public static class LocationAlertType
{
    public const string ZoneArrival           = "ZoneArrival";
    public const string ZoneDeparture         = "ZoneDeparture";
    public const string LateAlert             = "LateAlert";
    public const string SOS                   = "SOS";
    public const string BatteryWarning        = "BatteryWarning";
    public const string LocationStale         = "LocationStale";
    public const string LocationSharingPaused = "LocationSharingPaused";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ZoneArrival, ZoneDeparture, LateAlert, SOS,
        BatteryWarning, LocationStale, LocationSharingPaused
    };
}
