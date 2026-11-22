using System.Text.RegularExpressions;

namespace FamilyFirst.Application.Common.Models;

/// <summary>
/// TESTING ONLY. A default Child/Elder PIN accepted by POST /auth/verify-pin for users who have not set
/// their own PIN yet (no PIN-setting screen exists — see ProjectOverview §2.4).
/// Program.cs fills <see cref="DefaultPin"/> from "Auth:DevelopmentDefaultPin" ONLY when the environment is
/// Development; in every other environment it is null and the fallback is disabled.
/// A user who has their own PinHash must always use it — the default never overrides a real PIN.
/// </summary>
public sealed class DevelopmentPinSettings
{
    public DevelopmentPinSettings(string? defaultPin)
    {
        DefaultPin = defaultPin is not null && Regex.IsMatch(defaultPin.Trim(), @"\A\d{4}\z")
            ? defaultPin.Trim()
            : null;
    }

    public string? DefaultPin { get; }

    public bool IsEnabled => DefaultPin is not null;
}
