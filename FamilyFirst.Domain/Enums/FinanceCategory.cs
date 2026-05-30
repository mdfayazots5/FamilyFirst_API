namespace FamilyFirst.Domain.Enums;

public static class FinanceCategory
{
    public const string GroceriesKirana    = "GroceriesKirana";
    public const string FoodDining         = "FoodDining";
    public const string Utilities          = "Utilities";
    public const string MobileRecharge     = "MobileRecharge";
    public const string EducationSchool    = "EducationSchool";
    public const string MedicalHealth      = "MedicalHealth";
    public const string TravelTransport    = "TravelTransport";
    public const string Shopping           = "Shopping";
    public const string InsuranceLIC       = "InsuranceLIC";
    public const string LoanEmi            = "LoanEmi";
    public const string DomesticHelp       = "DomesticHelp";
    public const string Entertainment      = "Entertainment";
    public const string DonationsReligion  = "DonationsReligion";
    public const string ChitFundInvestment = "ChitFundInvestment";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        GroceriesKirana, FoodDining, Utilities, MobileRecharge,
        EducationSchool, MedicalHealth, TravelTransport, Shopping,
        InsuranceLIC, LoanEmi, DomesticHelp, Entertainment,
        DonationsReligion, ChitFundInvestment
    };

    // Categories blurred for Tier 2 members (personal/private spend)
    public static readonly IReadOnlySet<string> Tier2Blurred = new HashSet<string>
    {
        Entertainment, Shopping
    };
}
