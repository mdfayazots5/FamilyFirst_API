using System.Text.RegularExpressions;
using FamilyFirst.Application.Services.Interfaces;

namespace FamilyFirst.Application.Validators;

internal static class DatabaseRegex
{
    public static bool Matches(IRegexRuleProvider? provider, string method, string field, string? value, string fallback)
    {
        if (value is null)
            return true; // Required fields are handled by the existing NotEmpty rules.

        if (provider?.Find(method, field) is { } regex)
        {
            try { return regex.IsMatch(value); }
            catch (RegexMatchTimeoutException) { }
        }
        // Fallback patterns are code constants, but still bounded so no input can stall a request thread.
        return Regex.IsMatch(value, fallback, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}
