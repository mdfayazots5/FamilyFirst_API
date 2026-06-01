-- ============================================================
-- Script  : 082_SeedRolePermissions.sql
-- Purpose : Seed tblRolePermission — defines what each role can
--           do in each module. Based on CLAUDE.md Role Data Scope Rules.
--           This data is cached at startup and checked in every BAL method.
--
-- Role IDs:       SA=1, FA=2, PA=3, CH=4, TE=5, EL=6
-- Module IDs:     AUTH=1, FAMILY=2, DASH=3, ATTEND=4, TASK=5,
--                 FEEDBACK=6, REWARDS=7, CALENDAR=8, NOTIF=9, ADMIN=10
-- Permission IDs: V=1, CU=2, D=3, AR=4, AV=5
--
-- RULES FROM CLAUDE.md:
-- SuperAdmin   : All families via admin endpoints. Admin View + View on all modules.
-- FamilyAdmin  : All data within their FamilyId. All permissions within family scope.
-- Parent       : Own FamilyId data. V+CU+AR on core modules. D on tasks/rewards.
-- Child        : Own tasks, coins, rewards, streak. View only.
-- Teacher      : Own sessions and assigned children. CU on Attendance (time-gated in BAL).
--                CU on Feedback (24-hr window enforced in BAL).
-- Elder        : Grandchild summaries and family events. View only. No settings access.
-- Depends : 072_CreateRolePermission.sql
--           079_SeedRoles.sql, 080_SeedModules.sql, 078_SeedPermissions.sql
-- ============================================================

INSERT INTO dbo.tblRolePermission (RoleId, ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.RoleId, source.ModuleId, source.PermissionId, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- ── SuperAdmin (1): Admin View + View on ALL modules ─────────────────
    (1,  1, 1), (1,  1, 5),  -- AUTH
    (1,  2, 1), (1,  2, 5),  -- FAMILY
    (1,  3, 1), (1,  3, 5),  -- DASH
    (1,  4, 1), (1,  4, 5),  -- ATTEND
    (1,  5, 1), (1,  5, 5),  -- TASK
    (1,  6, 1), (1,  6, 5),  -- FEEDBACK
    (1,  7, 1), (1,  7, 5),  -- REWARDS
    (1,  8, 1), (1,  8, 5),  -- CALENDAR
    (1,  9, 1), (1,  9, 5),  -- NOTIF
    (1, 10, 1), (1, 10, 5),  -- ADMIN

    -- ── FamilyAdmin (2): All permissions within their FamilyId ────────────
    (2,  1, 1),              -- AUTH: View
    (2,  2, 1), (2,  2, 2), (2,  2, 3), (2,  2, 4),  -- FAMILY: V+CU+D+AR
    (2,  3, 1),              -- DASH: View
    (2,  4, 1), (2,  4, 2), (2,  4, 4),              -- ATTEND: V+CU+AR
    (2,  5, 1), (2,  5, 2), (2,  5, 3), (2,  5, 4),  -- TASK: V+CU+D+AR
    (2,  6, 1), (2,  6, 2), (2,  6, 3), (2,  6, 4),  -- FEEDBACK: V+CU+D+AR
    (2,  7, 1), (2,  7, 2), (2,  7, 3), (2,  7, 4),  -- REWARDS: V+CU+D+AR
    (2,  8, 1), (2,  8, 2), (2,  8, 3),              -- CALENDAR: V+CU+D
    (2,  9, 1), (2,  9, 2),                          -- NOTIF: V+CU
    (2, 10, 1), (2, 10, 2), (2, 10, 3),              -- ADMIN: V+CU+D

    -- ── Parent (3): Own FamilyId. V+CU+AR on core. D on tasks/rewards ─────
    (3,  1, 1),              -- AUTH: View
    (3,  2, 1),              -- FAMILY: View (cannot add/remove members — FamilyAdmin only)
    (3,  3, 1),              -- DASH: View
    (3,  4, 1), (3,  4, 2), (3,  4, 4),              -- ATTEND: V+CU+AR
    (3,  5, 1), (3,  5, 2), (3,  5, 3), (3,  5, 4),  -- TASK: V+CU+D+AR
    (3,  6, 1), (3,  6, 4),                          -- FEEDBACK: V+AR (parent can review)
    (3,  7, 1), (3,  7, 2), (3,  7, 3), (3,  7, 4),  -- REWARDS: V+CU+D+AR
    (3,  8, 1), (3,  8, 2), (3,  8, 3),              -- CALENDAR: V+CU+D
    (3,  9, 1), (3,  9, 2),                          -- NOTIF: V+CU (manage prefs)

    -- ── Child (4): Own data only. View only (write via task completion flow) ─
    (4,  1, 1),              -- AUTH: View
    (4,  3, 1),              -- DASH: View (own summary)
    (4,  5, 1), (4,  5, 2), -- TASK: V+CU (mark own tasks complete, upload photo)
    (4,  7, 1),              -- REWARDS: View own coins and redemptions
    (4,  8, 1),              -- CALENDAR: View family events
    (4,  9, 1),              -- NOTIF: View own notifications

    -- ── Teacher (5): Own sessions + assigned children only ────────────────
    -- Time-window enforcement (1hr for attendance, 24hr for feedback) is in BAL — not here.
    (5,  1, 1),              -- AUTH: View
    (5,  4, 1), (5,  4, 2), -- ATTEND: V+CU (session submit + 1hr edit — BAL enforced)
    (5,  6, 1), (5,  6, 2), -- FEEDBACK: V+CU (post + 24hr edit — BAL enforced)
    (5,  9, 1),              -- NOTIF: View own

    -- ── Elder (6): Read-only. Grandchild summaries + family events ─────────
    (6,  1, 1),              -- AUTH: View
    (6,  3, 1),              -- DASH: View (family summary)
    (6,  8, 1),              -- CALENDAR: View family events
    (6,  9, 1)               -- NOTIF: View own notifications
) AS source (RoleId, ModuleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblRolePermission t
    WHERE t.RoleId       = source.RoleId
      AND t.ModuleId     = source.ModuleId
      AND t.PermissionId = source.PermissionId
      AND t.IsDeleted    = 0
);
GO
