-- ============================================================
-- Script  : 103_SeedStaticAPITemplate.sql
-- Purpose : Register ALL FamilyFirst search and code SPs in
--           tblStaticAPITemplate. This is the complete mapping
--           that the generic GetDataBySearch / GetDataByCode API
--           controllers use to resolve stored procedure names.
--
-- PATTERN:
--   UI sends → { ModuleCode, MethodName, ...params }
--   API calls uspGetStaticAPITemplateByModuleCode(@ModuleCode, @MethodName)
--   Gets StoredProcedureName → executes SP with standard parameter set
--   Returns result to UI
--
-- MethodName naming convention:
--   GetDataBySearch       — primary list view for the module's main entity
--   GetDataByCode         — primary detail view (by GUID) for main entity
--   Get<Entity>BySearch   — list view for secondary entities within the module
--   Get<Entity>ById       — detail view for secondary entities
--
-- After individual GET APIs are removed, ALL gets go through these 2 endpoints.
-- Saving still uses individual POST/PUT/DELETE APIs — unchanged.
--
-- Depends : 091_CreateStaticAPITemplate.sql
--           094 through 101 (all Search/Code SPs)
-- ============================================================

-- Module IDs: AUTH=1, FAMILY=2, DASH=3, ATTEND=4, TASK=5,
--             FEEDBACK=6, REWARDS=7, CALENDAR=8, NOTIF=9, ADMIN=10

INSERT INTO dbo.tblStaticAPITemplate
    (StaticAPIMethodName, StoredProcedureName, StaticAPIType, StaticAPIMode,
     ModuleId, Comments, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT
    source.MethodName,
    source.SpName,
    N'POST',
    source.ApiMode,
    source.ModuleId,
    source.Comments,
    1, 1, 0, GETDATE(), N'System'
FROM (VALUES

    -- ── FAMILY module (ModuleId 2) ─────────────────────────────────────────
    -- Primary entity: Family
    (N'GetDataBySearch',            N'uspGetFamilyBySearch',                N'Search', 2, N'List families'),
    (N'GetDataByCode',              N'uspGetFamilyById',                    N'Code',   2, N'Get family by GUID'),
    -- FamilyMember
    (N'GetFamilyMemberBySearch',    N'uspGetFamilyMemberBySearch',          N'Search', 2, N'List family members'),
    (N'GetFamilyMemberById',        N'uspGetFamilyMemberById',              N'Code',   2, N'Get family member by GUID'),
    -- ChildProfile
    (N'GetChildProfileBySearch',    N'uspGetChildProfileBySearch',          N'Search', 2, N'List child profiles'),
    (N'GetChildProfileById',        N'uspGetChildProfileById',              N'Code',   2, N'Get child profile by GUID'),
    -- TeacherProfile
    (N'GetTeacherProfileBySearch',  N'uspGetTeacherProfileBySearch',        N'Search', 2, N'List teacher profiles'),
    (N'GetTeacherProfileById',      N'uspGetTeacherProfileById',            N'Code',   2, N'Get teacher profile by GUID'),

    -- ── ATTENDANCE module (ModuleId 4) ─────────────────────────────────────
    -- Primary entity: AttendanceSession
    (N'GetDataBySearch',            N'uspGetAttendanceSessionBySearch',     N'Search', 4, N'List attendance sessions'),
    (N'GetDataByCode',              N'uspGetAttendanceSessionById',         N'Code',   4, N'Get attendance session by GUID'),
    -- AttendanceRecord
    (N'GetAttendanceRecordBySearch',N'uspGetAttendanceRecordBySearch',      N'Search', 4, N'List attendance records'),
    (N'GetAttendanceRecordById',    N'uspGetAttendanceRecordById',          N'Code',   4, N'Get attendance record by GUID'),

    -- ── TASK module (ModuleId 5) ───────────────────────────────────────────
    -- Primary entity: TaskItem
    (N'GetDataBySearch',            N'uspGetTaskItemBySearch',              N'Search', 5, N'List task items'),
    (N'GetDataByCode',              N'uspGetTaskItemById',                  N'Code',   5, N'Get task item by GUID'),
    -- TaskCompletion
    (N'GetTaskCompletionBySearch',  N'uspGetTaskCompletionBySearch',        N'Search', 5, N'List task completions'),
    (N'GetTaskCompletionById',      N'uspGetTaskCompletionById',            N'Code',   5, N'Get task completion by GUID'),

    -- ── FEEDBACK module (ModuleId 6) ───────────────────────────────────────
    (N'GetDataBySearch',            N'uspGetTeacherFeedbackBySearch',       N'Search', 6, N'List teacher feedback'),
    (N'GetDataByCode',              N'uspGetTeacherFeedbackById',           N'Code',   6, N'Get teacher feedback by GUID'),

    -- ── REWARDS module (ModuleId 7) ────────────────────────────────────────
    -- Primary entity: Reward
    (N'GetDataBySearch',            N'uspGetRewardBySearch',                N'Search', 7, N'List rewards'),
    (N'GetDataByCode',              N'uspGetRewardById',                    N'Code',   7, N'Get reward by GUID'),
    -- RewardRedemption
    (N'GetRewardRedemptionBySearch',N'uspGetRewardRedemptionBySearch',      N'Search', 7, N'List reward redemptions'),
    (N'GetRewardRedemptionById',    N'uspGetRewardRedemptionById',          N'Code',   7, N'Get reward redemption by GUID'),
    -- CoinTransaction
    (N'GetCoinTransactionBySearch', N'uspGetCoinTransactionBySearch',       N'Search', 7, N'List coin transactions'),
    (N'GetCoinTransactionById',     N'uspGetCoinTransactionById',           N'Code',   7, N'Get coin transaction by GUID'),

    -- ── CALENDAR module (ModuleId 8) ───────────────────────────────────────
    (N'GetDataBySearch',            N'uspGetCalendarEventBySearch',         N'Search', 8, N'List calendar events'),
    (N'GetDataByCode',              N'uspGetCalendarEventById',             N'Code',   8, N'Get calendar event by GUID'),

    -- ── NOTIFICATION module (ModuleId 9) ───────────────────────────────────
    (N'GetDataBySearch',            N'uspGetNotificationBySearch',          N'Search', 9, N'List notifications'),
    (N'GetDataByCode',              N'uspGetNotificationById',              N'Code',   9, N'Get notification by GUID'),

    -- ── ADMIN module (ModuleId 10) ─────────────────────────────────────────
    (N'GetDataBySearch',            N'uspGetFeatureFlagBySearch',           N'Search', 10, N'List feature flags'),
    (N'GetDataByCode',              N'uspGetFeatureFlagById',               N'Code',   10, N'Get feature flag by GUID'),

    -- ── GLOBAL: MasterData (no module scope) ───────────────────────────────
    -- ModuleId = NULL → global, accessible from any module
    (N'GetMasterDataByCode',        N'uspGetMasterDataByCode',              N'Code',   NULL, N'Get master data dropdown by code')

) AS source (MethodName, SpName, ApiMode, ModuleId, Comments)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblStaticAPITemplate t
    WHERE t.StaticAPIMethodName = source.MethodName
      AND (
            (t.ModuleId = source.ModuleId)
            OR (t.ModuleId IS NULL AND source.ModuleId IS NULL)
          )
      AND t.IsDeleted = 0
);
GO
