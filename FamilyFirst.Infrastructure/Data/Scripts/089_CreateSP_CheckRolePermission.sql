SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : Checks whether a given role has a specific permission on a specific module.
--                    Called in BAL before every write/approve/delete operation — NOT just at the
--                    controller [Authorize(Roles=...)] level.
--                    Returns IsAuthorized = 1 if the role-module-permission combination exists
--                    and is active. Returns 0 if not found (BAL sets error code 7 = Permission_Denied).
--                    This SP result is cached per (RoleId, ModuleId) at startup by CacheWarmupService.
-- Usage            : EXEC dbo.uspCheckRolePermission @RoleId = 3, @ModuleId = 4, @PermissionId = 2
-- Input Parameters : @RoleId       — from FamilyFirstEnums.cs Role enum (e.g. Parent = 3)
--                    @ModuleId     — from FamilyFirstEnums.cs Module enum (e.g. Attendance = 4)
--                    @PermissionId — from FamilyFirstEnums.cs Permissions enum (e.g. CreateUpdate = 2)
-- Output           : IsAuthorized BIT — 1 = authorized, 0 = not authorized
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspCheckRolePermission
(
    @RoleId         BIGINT = 0,
    @ModuleId       BIGINT = 0,
    @PermissionId   BIGINT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM dbo.tblRolePermission rp WITH (NOLOCK)
                WHERE rp.RoleId       = @RoleId
                  AND rp.ModuleId     = @ModuleId
                  AND rp.PermissionId = @PermissionId
                  AND rp.IsDeleted    = 0
                  AND rp.IsPublished  = 1
            )
            THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS IsAuthorized;
END
GO
