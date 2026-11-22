SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (403 "You do not have permission" review)
-- Date Created     : 22 Nov 2026
-- Description      : Adds tblRolePermission rows that services already check but 082 never granted, which
--                    made the API return 403 Permission_Denied (ProjectOverview Drift Entry 087):
--                      (1,10,3) SuperAdmin   · AdminConfiguration · Delete       -> block a family
--                      (2, 4,3) FamilyAdmin  · Attendance         · Delete       -> delete comment template
--                      (5, 6,3) Teacher      · Feedback           · Delete       -> delete own feedback (24h rule)
--                      (4|5|6, 9,2) Child/Teacher/Elder · Notifications · CreateUpdate -> save OWN notification prefs
--                      (1..6, 1,2) every role · Authentication · CreateUpdate -> update OWN profile / FCM token
--                    Ownership / time-window rules stay enforced in the services. Idempotent.
--                    IPermissionService caches role permissions at startup — restart the API after running.
-- Usage            : sqlcmd -I -b -i 125_SeedMissingRolePermissions.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      22 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------

INSERT INTO dbo.tblRolePermission
    (RoleId, ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.RoleId, source.ModuleId, source.PermissionId, 1, 1, 0, GETUTCDATE(), N'System'
FROM (VALUES
    (1, 10, 3),                                                  -- SuperAdmin · ADMIN · Delete
    (2,  4, 3),                                                  -- FamilyAdmin · ATTEND · Delete
    (5,  6, 3),                                                  -- Teacher · FEEDBACK · Delete
    (4,  9, 2), (5,  9, 2), (6,  9, 2),                          -- Child/Teacher/Elder · NOTIF · CreateUpdate
    (1,  1, 2), (2,  1, 2), (3,  1, 2), (4,  1, 2), (5,  1, 2), (6,  1, 2)  -- all roles · AUTH · CreateUpdate
) AS source (RoleId, ModuleId, PermissionId)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.tblRolePermission AS existing WITH (UPDLOCK, HOLDLOCK)
    WHERE existing.RoleId       = source.RoleId
      AND existing.ModuleId     = source.ModuleId
      AND existing.PermissionId = source.PermissionId
      AND existing.IsDeleted    = 0
);
GO
