namespace FamilyFirst.Domain.Enums;

/// <summary>
/// Matches the PermissionId values seeded in tblPermission (078_SeedPermissions.sql).
/// Used by BAL to call uspCheckRolePermission(roleId, moduleId, permissionId).
/// Each service method calls CheckAsync with the appropriate permission before write operations.
/// </summary>
public enum FamilyFirstPermission
{
    View            = 1,
    CreateUpdate    = 2,
    Delete          = 3,
    ApproveReject   = 4,
    AdminView       = 5
}
