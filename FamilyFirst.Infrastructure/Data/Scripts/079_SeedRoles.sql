-- ============================================================
-- Script  : 079_SeedRoles.sql
-- Purpose : Seed tblRole with FamilyFirst's 6 canonical roles.
--           RoleId values match the Role enum in FamilyFirstEnums.cs
--           and the role constants used throughout the backend.
--           Per CLAUDE.md: SuperAdmin=1, FamilyAdmin=2, Parent=3,
--           Child=4, Teacher=5, Elder=6.
-- Depends : 068_CreateRole.sql
-- ============================================================

SET IDENTITY_INSERT dbo.tblRole ON;
GO

MERGE dbo.tblRole AS target
USING (VALUES
    (1, N'SuperAdmin',   N'SA', N'App Owner / Platform Operator. Views all families via admin endpoints. Cannot view Document Vault or Medical Records.',             1),
    (2, N'FamilyAdmin',  N'FA', N'Head of Family. Views all data within their FamilyId scope. Cannot see other families.',                                            2),
    (3, N'Parent',       N'PA', N'Mother / Father. Views own FamilyId data. Manages children''s attendance, tasks, feedback for ChildProfiles in own family only.',  3),
    (4, N'Child',        N'CH', N'Son / Daughter (age 5-17). Views own tasks, coins, rewards, streak only.',                                                          4),
    (5, N'Teacher',      N'TE', N'School / Tuition / Subject Teacher. Views own sessions and assigned children only.',                                                5),
    (6, N'Elder',        N'EL', N'Grandparent / Uncle / Aunt. Read-only access to grandchild summaries and family events.',                                           6)
) AS source (RoleId, RoleName, RoleCode, RoleDescription, SortOrder)
ON target.RoleId = source.RoleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, RoleName, RoleCode, RoleDescription, SortOrder,
            IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
    VALUES (source.RoleId, source.RoleName, source.RoleCode,
            source.RoleDescription, source.SortOrder,
            1, 1, 0, GETDATE(), N'System');
GO

SET IDENTITY_INSERT dbo.tblRole OFF;
GO
