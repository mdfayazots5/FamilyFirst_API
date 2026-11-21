namespace FamilyFirst.Domain.Enums;

/// <summary>
/// Matches the ModuleId values seeded in tblModule (080_SeedModules.sql).
/// Used by BAL to call uspCheckRolePermission(roleId, moduleId, permissionId).
/// </summary>
public enum FamilyFirstModule
{
    Authentication      = 1,
    Family              = 2,
    Dashboard           = 3,
    Attendance          = 4,
    Task                = 5,
    Feedback            = 6,
    Rewards             = 7,
    Calendar            = 8,
    Notifications       = 9,
    AdminConfiguration  = 10,

    // ── Level 2 modules (seeded in 108_SeedModules_Level2.sql) ──────────────
    // SuperAdmin has NO role-permission rows on 11–15 (ProjectOverview §18.2 — absolute; verified in 110 seed).
    DocumentVault       = 11,
    MedicalRecords      = 12,
    Safety              = 13,
    Finance             = 14,
    Reports             = 15
}
