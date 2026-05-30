namespace FamilyFirst.Domain.Enums;

public static class HealthRecordEventType
{
    public const string Prescription  = "Prescription";
    public const string Vaccination   = "Vaccination";
    public const string HospitalVisit = "HospitalVisit";
    public const string TestReport    = "TestReport";
    public const string DoctorNote    = "DoctorNote";
    public const string AllergyUpdate = "AllergyUpdate";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Prescription, Vaccination, HospitalVisit, TestReport, DoctorNote, AllergyUpdate
    };
}
