using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed class UpdateModuleVisibilityRequest
{
    public IReadOnlyCollection<ModuleVisibilityUpdateItem> Items { get; init; } = Array.Empty<ModuleVisibilityUpdateItem>();
}

public sealed class ModuleVisibilityUpdateItem
{
    public UserRole Role { get; init; }

    public string ModuleName { get; init; } = string.Empty;

    public bool IsVisible { get; init; }
}
