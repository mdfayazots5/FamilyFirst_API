-- ============================================================
-- Script  : 083_SeedMasterData.sql
-- Purpose : Seed tblMasterData with category header rows only.
--
-- PATTERN (from RevalPOS_RevalERPlocalDB):
--   Every row in tblMasterData (IsMasterData = 0) is a POINTER to a table.
--     MasterDataName = actual table name (e.g. 'tblRole', 'tblTaskType')
--     MasterDataCode = code the UI/API sends (e.g. 'Role', 'TaskType')
--     MasterCodeSpName = custom SP name if needed; NULL = main SP routes directly
--   No inline values are stored here. All data comes from dedicated tables.
--   The SP routes each MasterDataCode to its specific table.
--
-- Depends : 073_CreateMasterData.sql
--           Scripts 067–072 (tblRole, tblModule, tblPermission, etc.)
--           092_CreateLookupTables.sql (tblTaskType, tblAttendanceStatus, etc.)
-- ============================================================

INSERT INTO dbo.tblMasterData
    (MasterDataName, MasterDataCode, IsMasterData, MasterCodeSpName,
     SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT
    source.MasterDataName, source.MasterDataCode, 0, source.MasterCodeSpName,
    source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- System/admin tables — no FamilyId scope
    (N'tblFamily',                      N'Family',                      NULL,  0),
    (N'tblRole',                        N'Role',                        NULL,  1),
    (N'tblModule',                      N'Module',                      NULL,  2),
    (N'tblPermission',                  N'Permission',                  NULL,  3),
    (N'tblPlan',                        N'Plan',                        NULL,  4),
    (N'tblUser',                        N'User',                        NULL,  5),
    -- Family-scoped entity tables
    (N'tblFamilyMember',                N'FamilyMember',                NULL,  6),
    (N'tblChildProfile',                N'ChildProfile',                NULL,  7),
    (N'tblTeacherProfile',              N'TeacherProfile',              NULL,  8),
    (N'tblCustomAttendanceStatuses',    N'CustomAttendanceStatus',      NULL,  9),
    (N'tblReward',                      N'Reward',                      NULL, 10),
    -- Simple lookup tables (created in 092_CreateLookupTables.sql)
    (N'tblTaskType',                    N'TaskType',                    NULL, 11),
    (N'tblTaskStatus',                  N'TaskStatus',                  NULL, 12),
    (N'tblAttendanceStatus',            N'AttendanceStatus',            NULL, 13),
    (N'tblRewardType',                  N'RewardType',                  NULL, 14),
    (N'tblCoinTransactionType',         N'CoinTransactionType',         NULL, 15),
    (N'tblFeedbackRating',              N'FeedbackRating',              NULL, 16),
    (N'tblCalendarEventType',           N'CalendarEventType',           NULL, 17),
    (N'tblNotificationType',            N'NotificationType',            NULL, 18),
    (N'tblOTPType',                     N'OTPType',                     NULL, 19)
) AS source (MasterDataName, MasterDataCode, MasterCodeSpName, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblMasterData t
    WHERE t.MasterDataCode = source.MasterDataCode
      AND t.IsDeleted      = 0
);
GO
