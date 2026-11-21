-- ============================================================
-- Script  : 109_SeedModulePermissions_Level2.sql
-- Purpose : Seed tblModulePermission for the 5 Level 2 modules —
--           which permission types apply to each module.
--
-- Admin View (AV=5) is deliberately NOT applicable to VAULT, MEDICAL,
-- SAFETY or FINANCE: SuperAdmin cannot view Document Vault, Medical,
-- Location or Finance data — absolute, enforced at DB layer
-- (ProjectOverview §18.2).
--
-- Depends : 071_CreateModulePermission.sql, 078_SeedPermissions.sql,
--           108_SeedModules_Level2.sql
-- Idempotent: NOT EXISTS guard — safe to re-run.
-- ============================================================
-- Permission IDs:  V=1, CU=2, D=3, AR=4, AV=5
-- Module IDs:      VAULT=11, MEDICAL=12, SAFETY=13, FINANCE=14, REPORTS=15
-- ============================================================

INSERT INTO dbo.tblModulePermission (ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.ModuleId, source.PermissionId, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- VAULT: View, CU (upload/update/share), Delete
    (11, 1), (11, 2), (11, 3),
    -- MEDICAL: View, CU (records, emergency card share), Delete
    (12, 1), (12, 2), (12, 3),
    -- SAFETY: View, CU (zones, consent, SOS trigger), Delete, Approve/Reject (SOS resolve)
    (13, 1), (13, 2), (13, 3), (13, 4),
    -- FINANCE: View, CU (consent, budget, transaction questions), Delete
    (14, 1), (14, 2), (14, 3),
    -- REPORTS: View only (read-only aggregations; export is a read operation)
    (15, 1)
) AS source (ModuleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblModulePermission t
    WHERE t.ModuleId     = source.ModuleId
      AND t.PermissionId = source.PermissionId
      AND t.IsDeleted    = 0
);
GO
