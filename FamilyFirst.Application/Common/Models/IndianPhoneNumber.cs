using System.Text.RegularExpressions;

namespace FamilyFirst.Application.Common.Models;

/// <summary>
/// FamilyFirst is India-only (founder decision 2026-11-22): every phone number is an Indian mobile
/// number — 10 digits starting with 6, 7, 8 or 9 — stored in E.164 form as +91XXXXXXXXXX.
/// Clients may send the 10 digits alone or with the +91 prefix.
/// </summary>
public static class IndianPhoneNumber
{
    public const string CountryCode = "+91";

    /// <summary>Accepts "9876543210" or "+919876543210".</summary>
    public const string Pattern = @"^(\+91)?[6-9]\d{9}$";

    public const string ValidationMessage = "Enter a valid 10-digit Indian mobile number starting with 6, 7, 8 or 9.";

    public static bool IsValid(string? value)
    {
        return value is not null
            && Regex.IsMatch(value.Trim(), Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    /// <summary>Returns +91 followed by the 10 national digits. Call only after validation.</summary>
    public static string Normalize(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();
        var nationalDigits = trimmed.StartsWith(CountryCode, StringComparison.Ordinal)
            ? trimmed[CountryCode.Length..]
            : trimmed;

        return $"{CountryCode}{nationalDigits}";
    }
}
