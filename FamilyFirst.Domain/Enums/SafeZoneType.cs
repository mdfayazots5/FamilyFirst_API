namespace FamilyFirst.Domain.Enums;

public static class SafeZoneType
{
    public const string Home           = "Home";
    public const string School         = "School";
    public const string Tuition        = "Tuition";
    public const string RelativesHouse = "RelativesHouse";
    public const string Workplace      = "Workplace";
    public const string PlaceOfWorship = "PlaceOfWorship";
    public const string Other          = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Home, School, Tuition, RelativesHouse, Workplace, PlaceOfWorship, Other
    };
}
