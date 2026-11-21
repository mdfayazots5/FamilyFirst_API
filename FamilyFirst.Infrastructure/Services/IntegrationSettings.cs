using System.Diagnostics.CodeAnalysis;

namespace FamilyFirst.Infrastructure.Services;

public static class IntegrationSettings
{
    public static bool IsConfigured([NotNullWhen(true)] string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !value.TrimStart().StartsWith('<')
            && !string.Equals(value.Trim(), "PLACEHOLDER", StringComparison.OrdinalIgnoreCase);
    }
}
