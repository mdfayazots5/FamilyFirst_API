SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst
-- Date Created     : 22 Nov 2026
-- Description      : Adds the missing SuperAdmin CreateUpdate grant for AdminConfiguration.
-- Usage            : sqlcmd -I -b -i 123_SeedSuperAdminConfigurationWritePermission.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------

INSERT INTO dbo.tblRolePermission
    (RoleId, ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT 1, 10, 2, 1, 1, 0, GETUTCDATE(), N'System'
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.tblRolePermission WITH (UPDLOCK, HOLDLOCK)
    WHERE RoleId = 1
        AND ModuleId = 10
        AND PermissionId = 2
        AND IsDeleted = 0
);
GO
