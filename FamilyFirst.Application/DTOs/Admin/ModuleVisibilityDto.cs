using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed record ModuleVisibilityDto(
    Guid? ConfigId,
    UserRole Role,
    string ModuleName,
    bool IsVisible,
    bool IsDefault,
    DateTime UpdatedAt);
