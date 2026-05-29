using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed record FeatureFlagDto(
    string FlagKey,
    string FlagValue,
    string? Description,
    DateTime UpdatedAt);

public sealed class UpdateFeatureFlagRequest
{
    public string FlagValue { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class NotificationCampaignRequest
{
    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> PlanCodes { get; init; } = Array.Empty<string>();

    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    public string? DeepLinkPath { get; init; }

    public DateTime? ScheduledFor { get; init; }
}

public sealed record NotificationCampaignResultDto(int RecipientCount);
