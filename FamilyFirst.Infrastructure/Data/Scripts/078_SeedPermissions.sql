-- ============================================================
-- Script  : 078_SeedPermissions.sql
-- Purpose : Seed tblPermission with the 5 operation-level
--           permission types used for BAL access control.
--           PermissionId values match Permissions enum in
--           FamilyFirstEnums.cs: View=1, CreateUpdate=2,
--           Delete=3, ApproveReject=4, AdminView=5.
-- Depends : 067_CreatePermission.sql
-- ============================================================

SET IDENTITY_INSERT dbo.tblPermission ON;
GO

MERGE dbo.tblPermission AS target
USING (VALUES
    (1, N'View',           N'V',  N'Read-only access to module data',                    1),
    (2, N'Create/Update',  N'CU', N'Create new records and update existing ones',         2),
    (3, N'Delete',         N'D',  N'Soft-delete records',                                3),
    (4, N'Approve/Reject', N'AR', N'Approve or reject pending actions (e.g. tasks)',      4),
    (5, N'Admin View',     N'AV', N'SuperAdmin-level view across all families/entities',  5)
) AS source (PermissionId, PermissionName, PermissionCode, Comments, SortOrder)
ON target.PermissionId = source.PermissionId
WHEN NOT MATCHED THEN
    INSERT (PermissionId, PermissionName, PermissionCode, Comments, SortOrder,
            IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
    VALUES (source.PermissionId, source.PermissionName, source.PermissionCode,
            source.Comments, source.SortOrder,
            1, 1, 0, GETDATE(), N'System');
GO

SET IDENTITY_INSERT dbo.tblPermission OFF;
GO
