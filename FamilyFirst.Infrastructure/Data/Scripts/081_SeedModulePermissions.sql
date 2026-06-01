-- ============================================================
-- Script  : 081_SeedModulePermissions.sql
-- Purpose : Seed tblModulePermission — maps which permission
--           types are applicable to each module.
--           E.g. Auth module only supports View (token validation).
--           Attendance supports Create/Update + Approve/Reject.
-- Depends : 071_CreateModulePermission.sql
--           078_SeedPermissions.sql
--           080_SeedModules.sql
-- ============================================================
-- Permission IDs:  V=1, CU=2, D=3, AR=4, AV=5
-- Module IDs:      AUTH=1, FAMILY=2, DASH=3, ATTEND=4, TASK=5,
--                  FEEDBACK=6, REWARDS=7, CALENDAR=8, NOTIF=9, ADMIN=10
-- ============================================================

INSERT INTO dbo.tblModulePermission (ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.ModuleId, source.PermissionId, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- AUTH: View only (token check), Admin View for SuperAdmin
    (1, 1), (1, 5),
    -- FAMILY: View, CU, Delete, Admin View
    (2, 1), (2, 2), (2, 3), (2, 5),
    -- DASH: View, Admin View
    (3, 1), (3, 5),
    -- ATTEND: View, CU, Approve/Reject, Admin View
    (4, 1), (4, 2), (4, 4), (4, 5),
    -- TASK: View, CU, Delete, Approve/Reject, Admin View
    (5, 1), (5, 2), (5, 3), (5, 4), (5, 5),
    -- FEEDBACK: View, CU, Delete, Approve/Reject, Admin View
    (6, 1), (6, 2), (6, 3), (6, 4), (6, 5),
    -- REWARDS: View, CU, Delete, Approve/Reject, Admin View
    (7, 1), (7, 2), (7, 3), (7, 4), (7, 5),
    -- CALENDAR: View, CU, Delete, Admin View
    (8, 1), (8, 2), (8, 3), (8, 5),
    -- NOTIF: View, Admin View
    (9, 1), (9, 5),
    -- ADMIN: View, CU, Delete, Admin View
    (10, 1), (10, 2), (10, 3), (10, 5)
) AS source (ModuleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblModulePermission t
    WHERE t.ModuleId = source.ModuleId
      AND t.PermissionId = source.PermissionId
      AND t.IsDeleted = 0
);
GO
