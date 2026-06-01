using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

/// <summary>
/// Fine-grained per-operation permission check service.
/// Wraps uspCheckRolePermission — called in BAL before every write, approve, or delete.
/// This is in ADDITION to the controller-level [Authorize] attribute:
///   [Authorize] = JWT is valid and user is authenticated
///   IPermissionService.CheckAsync = role has permission for THIS specific operation on THIS module
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns true if the given role has the specified permission on the given module.
    /// Returns false if not — caller throws ForbiddenAccessException.
    /// </summary>
    Task<bool> CheckAsync(
        UserRole role,
        FamilyFirstModule module,
        FamilyFirstPermission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Overload that accepts role name string from JWT claims.
    /// Parses the string to UserRole enum internally.
    /// Returns false (not authorized) if the role string is unrecognised.
    /// </summary>
    Task<bool> CheckAsync(
        string roleName,
        FamilyFirstModule module,
        FamilyFirstPermission permission,
        CancellationToken cancellationToken = default);
}
