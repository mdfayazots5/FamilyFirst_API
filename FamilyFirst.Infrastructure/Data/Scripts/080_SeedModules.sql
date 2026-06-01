-- ============================================================
-- Script  : 080_SeedModules.sql
-- Purpose : Seed tblModule with FamilyFirst's 10 Level 1 modules.
--           ModuleId values match the Module enum in FamilyFirstEnums.cs
--           and the Build Phase order from CLAUDE.md.
-- Depends : 069_CreateModule.sql
-- ============================================================

SET IDENTITY_INSERT dbo.tblModule ON;
GO

MERGE dbo.tblModule AS target
USING (VALUES
    ( 1, N'Authentication',     N'AUTH',     NULL, N'Phone OTP login, JWT, refresh token, PIN auth for Child and Elder roles.',              1),
    ( 2, N'Family Management',  N'FAMILY',   NULL, N'Family creation, member management, join codes, FamilyAdmin administration.',           2),
    ( 3, N'Family Dashboard',   N'DASH',     NULL, N'Family overview, summary cards, member status, daily highlights.',                      3),
    ( 4, N'Attendance',         N'ATTEND',   NULL, N'Teacher session submission, parent review, 1-hour edit window enforcement.',             4),
    ( 5, N'Tasks',              N'TASK',     NULL, N'Daily routines and tasks for children. Photo proof, parent approval, coin earn.',        5),
    ( 6, N'Feedback',           N'FEEDBACK', NULL, N'Teacher feedback on children. 24-hour edit window. Parent notification.',               6),
    ( 7, N'Rewards',            N'REWARDS',  NULL, N'Reward catalogue, coin redemption, idempotency guard, system and custom rewards.',       7),
    ( 8, N'Calendar',           N'CALENDAR', NULL, N'Family events, school dates, reminders. All-role visibility with role-based editing.',  8),
    ( 9, N'Notifications',      N'NOTIF',    NULL, N'FCM push notifications, in-app alerts, preference settings.',                           9),
    (10, N'Admin Configuration',N'ADMIN',    NULL, N'SuperAdmin and FamilyAdmin configuration, plan management, feature flags.',             10)
) AS source (ModuleId, ModuleName, ModuleCode, ParentModuleId, Comments, SortOrder)
ON target.ModuleId = source.ModuleId
WHEN NOT MATCHED THEN
    INSERT (ModuleId, ModuleName, ModuleCode, ParentModuleId, Comments, SortOrder,
            IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
    VALUES (source.ModuleId, source.ModuleName, source.ModuleCode,
            source.ParentModuleId, source.Comments, source.SortOrder,
            1, 1, 0, GETDATE(), N'System');
GO

SET IDENTITY_INSERT dbo.tblModule OFF;
GO
