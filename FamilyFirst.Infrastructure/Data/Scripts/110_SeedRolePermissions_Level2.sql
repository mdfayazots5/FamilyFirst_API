-- ============================================================
-- Script  : 110_SeedRolePermissions_Level2.sql
-- Purpose : Seed tblRolePermission for the 5 Level 2 modules.
--           Source of truth: ProjectOverview.md §18.3
--           "Level 2 — Module Access" matrix + §18.2 data scope rules.
--
-- Role IDs:       SA=1, FA=2, PA=3, CH=4, TE=5, EL=6
-- Module IDs:     VAULT=11, MEDICAL=12, SAFETY=13, FINANCE=14, REPORTS=15
-- Permission IDs: V=1, CU=2, D=3, AR=4, AV=5
--
-- MATRIX → PERMISSION MAPPING (§18.3):
-- Document Vault — all operations          : FA ✓, P ✓                 → FA/P: V+CU+D
-- Medical Records — read / write           : FA ✓, P ✓, C Own          → FA/P: V+CU+D, C: V+CU (own only — BAL enforced)
-- Medical Emergency Card — share           : FA ✓, P ✓                 → covered by CU above
-- Safety — configure zones                 : FA ✓, P ✓                 → FA/P: CU+D
-- Safety — live view                       : FA ✓, P ✓, C own, E ✓     → FA/P/C/E: V (C own, E grandchild — BAL enforced)
-- Safety — SOS trigger                     : C ✓                       → C: CU (SOS trigger only — BAL enforced)
-- Finance — consent & data view            : FA ✓ (CFO), P own         → FA/P: V
-- Finance — manage transactions            : FA ✓, P own               → FA: CU+D, P: CU (own only — BAL enforced)
-- Level 2 Reports                          : FA ✓, P ✓                 → FA/P: V
--
-- SuperAdmin (1) receives NO rows on modules 11–15. Absolute rule (§18.2):
--   SuperAdmin cannot view Document Vault, Medical, Location or Finance data.
--   Level 2 Reports matrix row also shows SuperAdmin as "—".
-- Teacher (5) receives NO rows on modules 11–15 (matrix shows "—" everywhere).
--
-- [VERIFY] Rows not stated explicitly in the matrix (derived — confirm with Product):
--   (2,13,4),(3,13,4)  Safety Approve/Reject = SOS resolve/acknowledge by FamilyAdmin/Parent.
--   (2,14,3)           Finance Delete for FamilyAdmin (CFO) — "manage transactions".
--   Parent Finance Delete intentionally NOT granted (matrix says "Own" for manage only).
--
-- Depends : 072_CreateRolePermission.sql, 079_SeedRoles.sql,
--           078_SeedPermissions.sql, 108_SeedModules_Level2.sql
-- Idempotent: NOT EXISTS guard — safe to re-run.
-- ============================================================

INSERT INTO dbo.tblRolePermission (RoleId, ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.RoleId, source.ModuleId, source.PermissionId, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- ── FamilyAdmin (2) ─────────────────────────────────────────────────
    (2, 11, 1), (2, 11, 2), (2, 11, 3),              -- VAULT:   V+CU+D
    (2, 12, 1), (2, 12, 2), (2, 12, 3),              -- MEDICAL: V+CU+D
    (2, 13, 1), (2, 13, 2), (2, 13, 3), (2, 13, 4),  -- SAFETY:  V+CU+D+AR  [VERIFY AR]
    (2, 14, 1), (2, 14, 2), (2, 14, 3),              -- FINANCE: V+CU+D     [VERIFY D]
    (2, 15, 1),                                      -- REPORTS: V

    -- ── Parent (3) ──────────────────────────────────────────────────────
    (3, 11, 1), (3, 11, 2), (3, 11, 3),              -- VAULT:   V+CU+D
    (3, 12, 1), (3, 12, 2), (3, 12, 3),              -- MEDICAL: V+CU+D
    (3, 13, 1), (3, 13, 2), (3, 13, 3), (3, 13, 4),  -- SAFETY:  V+CU+D+AR  [VERIFY AR]
    (3, 14, 1), (3, 14, 2),                          -- FINANCE: V+CU (own consent / own transactions — BAL enforced)
    (3, 15, 1),                                      -- REPORTS: V

    -- ── Child (4) ───────────────────────────────────────────────────────
    (4, 12, 1), (4, 12, 2),                          -- MEDICAL: V+CU (own records only — BAL enforced)
    (4, 13, 1), (4, 13, 2),                          -- SAFETY:  V (own view) + CU (SOS trigger only — BAL enforced)

    -- ── Elder (6) ───────────────────────────────────────────────────────
    (6, 13, 1)                                       -- SAFETY:  V (grandchild live view — BAL enforced)
) AS source (RoleId, ModuleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblRolePermission t
    WHERE t.RoleId       = source.RoleId
      AND t.ModuleId     = source.ModuleId
      AND t.PermissionId = source.PermissionId
      AND t.IsDeleted    = 0
);
GO
