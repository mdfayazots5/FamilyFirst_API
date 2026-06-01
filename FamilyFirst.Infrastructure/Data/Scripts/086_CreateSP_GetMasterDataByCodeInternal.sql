SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : BAL-internal GUID validator. Validates the GUID from UI against its dedicated table
--                    and returns the INT PK. The INT PK is then passed to save stored procedures.
--                    UI NEVER sees INT PKs — only GUIDs via uspGetMasterDataByCode.
--
-- Every MasterDataCode routes to its own dedicated table (reference pattern).
-- FamilyId-scoped codes additionally validate that the GUID belongs to the caller's family.
-- All INT PKs are aliased as [MasterDataId] so BAL reads one uniform column name.
--
-- Empty result = invalid GUID or FamilyId mismatch → BAL sets error 23 (Invalid_MasterData).
-- NO hardcoded error messages or return codes — see Section 6B of New SQL Format.txt.
--
-- Usage : EXEC dbo.uspGetMasterDataByCodeInternal @MasterDataCode = 'TaskType',
--                                                  @GuidValue = 'A1B2C3D4-...'
--         EXEC dbo.uspGetMasterDataByCodeInternal @MasterDataCode = 'ChildProfile',
--                                                  @GuidValue = 'B2C3D4E5-...', @FamilyId = 42
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation — all FamilyFirst L1 tables
-- 1.1       Claude Project AI Engineer 01 Jun 2026    All codes route to dedicated tables; no fallback
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspGetMasterDataByCodeInternal
(
    @MasterDataCode     NVARCHAR(64)    = NULL,
    @GuidValue          NVARCHAR(64)    = NULL,
    @LanguageId         INT             = 1,
    @FamilyId           BIGINT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ParsedGuid UNIQUEIDENTIFIER = TRY_CAST(@GuidValue AS UNIQUEIDENTIFIER);

    IF @ParsedGuid IS NULL
        RETURN;

    DECLARE @RoleCode                   NVARCHAR(64) = N'Role';
    DECLARE @ModuleCode                 NVARCHAR(64) = N'Module';
    DECLARE @PermissionCode             NVARCHAR(64) = N'Permission';
    DECLARE @FamilyCode                 NVARCHAR(64) = N'Family';
    DECLARE @PlanCode                   NVARCHAR(64) = N'Plan';
    DECLARE @UserCode                   NVARCHAR(64) = N'User';
    DECLARE @FamilyMemberCode           NVARCHAR(64) = N'FamilyMember';
    DECLARE @ChildProfileCode           NVARCHAR(64) = N'ChildProfile';
    DECLARE @TeacherProfileCode         NVARCHAR(64) = N'TeacherProfile';
    DECLARE @CustomAttendanceStatusCode NVARCHAR(64) = N'CustomAttendanceStatus';
    DECLARE @RewardCode                 NVARCHAR(64) = N'Reward';
    DECLARE @TaskTypeCode               NVARCHAR(64) = N'TaskType';
    DECLARE @TaskStatusCode             NVARCHAR(64) = N'TaskStatus';
    DECLARE @AttendanceStatusCode       NVARCHAR(64) = N'AttendanceStatus';
    DECLARE @RewardTypeCode             NVARCHAR(64) = N'RewardType';
    DECLARE @CoinTransactionTypeCode    NVARCHAR(64) = N'CoinTransactionType';
    DECLARE @FeedbackRatingCode         NVARCHAR(64) = N'FeedbackRating';
    DECLARE @CalendarEventTypeCode      NVARCHAR(64) = N'CalendarEventType';
    DECLARE @NotificationTypeCode       NVARCHAR(64) = N'NotificationType';
    DECLARE @OTPTypeCode                NVARCHAR(64) = N'OTPType';

    -- ── Family → tblFamily.FamilyId ──────────────────────────────────────
    IF @MasterDataCode = @FamilyCode
        SELECT TOP 1 f.FamilyId AS MasterDataId
        FROM dbo.tblFamily f WITH (NOLOCK)
        WHERE f.IsDeleted = 0 AND f.Id = @ParsedGuid;

    -- ── Role → tblRole.RoleId ─────────────────────────────────────────────
    ELSE IF @MasterDataCode = @RoleCode
        SELECT TOP 1 r.RoleId AS MasterDataId
        FROM dbo.tblRole r WITH (NOLOCK)
        WHERE r.IsDeleted = 0 AND r.IsPublished = 1 AND r.Id = @ParsedGuid;

    -- ── Module → tblModule.ModuleId ───────────────────────────────────────
    ELSE IF @MasterDataCode = @ModuleCode
        SELECT TOP 1 m.ModuleId AS MasterDataId
        FROM dbo.tblModule m WITH (NOLOCK)
        WHERE m.IsDeleted = 0 AND m.IsPublished = 1 AND m.Id = @ParsedGuid;

    -- ── Permission → tblPermission.PermissionId ───────────────────────────
    ELSE IF @MasterDataCode = @PermissionCode
        SELECT TOP 1 p.PermissionId AS MasterDataId
        FROM dbo.tblPermission p WITH (NOLOCK)
        WHERE p.IsDeleted = 0 AND p.IsPublished = 1 AND p.Id = @ParsedGuid;

    -- ── Plan → tblPlan.PlanId ─────────────────────────────────────────────
    ELSE IF @MasterDataCode = @PlanCode
        SELECT TOP 1 pl.PlanId AS MasterDataId
        FROM dbo.tblPlan pl WITH (NOLOCK)
        WHERE pl.IsDeleted = 0 AND pl.IsPublished = 1 AND pl.Id = @ParsedGuid;

    -- ── User → tblUser.UserId ─────────────────────────────────────────────
    ELSE IF @MasterDataCode = @UserCode
        SELECT TOP 1 u.UserId AS MasterDataId
        FROM dbo.tblUser u WITH (NOLOCK)
        WHERE u.IsDeleted = 0 AND u.IsActive = 1 AND u.Id = @ParsedGuid;

    -- ── FamilyMember → tblFamilyMember.FamilyMemberId [FamilyId validated] ─
    ELSE IF @MasterDataCode = @FamilyMemberCode
        SELECT TOP 1 fm.FamilyMemberId AS MasterDataId
        FROM dbo.tblFamilyMember fm WITH (NOLOCK)
        WHERE fm.IsDeleted = 0 AND fm.IsActive = 1
          AND fm.Id = @ParsedGuid
          AND (@FamilyId = 0 OR fm.FamilyId = @FamilyId);

    -- ── ChildProfile → tblChildProfile.ChildProfileId [FamilyId validated] ─
    ELSE IF @MasterDataCode = @ChildProfileCode
        SELECT TOP 1 cp.ChildProfileId AS MasterDataId
        FROM dbo.tblChildProfile cp WITH (NOLOCK)
        WHERE cp.IsDeleted = 0
          AND cp.Id = @ParsedGuid
          AND (@FamilyId = 0 OR cp.FamilyId = @FamilyId);

    -- ── TeacherProfile → tblTeacherProfile.TeacherProfileId [FamilyId] ───
    ELSE IF @MasterDataCode = @TeacherProfileCode
        SELECT TOP 1 tp.TeacherProfileId AS MasterDataId
        FROM dbo.tblTeacherProfile tp WITH (NOLOCK)
        WHERE tp.IsDeleted = 0 AND tp.IsActive = 1
          AND tp.Id = @ParsedGuid
          AND (@FamilyId = 0 OR tp.FamilyId = @FamilyId);

    -- ── CustomAttendanceStatus → tblCustomAttendanceStatuses [FamilyId] ──
    ELSE IF @MasterDataCode = @CustomAttendanceStatusCode
        SELECT TOP 1 cas.CustomAttendanceStatusId AS MasterDataId
        FROM dbo.tblCustomAttendanceStatuses cas WITH (NOLOCK)
        WHERE cas.IsDeleted = 0
          AND cas.Id = @ParsedGuid
          AND (@FamilyId = 0 OR cas.FamilyId = @FamilyId);

    -- ── Reward → tblReward.RewardId [FamilyId + system rewards] ──────────
    ELSE IF @MasterDataCode = @RewardCode
        SELECT TOP 1 r.RewardId AS MasterDataId
        FROM dbo.tblReward r WITH (NOLOCK)
        WHERE r.IsDeleted = 0 AND r.IsPublished = 1
          AND r.Id = @ParsedGuid
          AND (@FamilyId = 0 OR r.FamilyId = @FamilyId OR r.FamilyId IS NULL);

    -- ── TaskType → tblTaskType.TaskTypeId ────────────────────────────────
    ELSE IF @MasterDataCode = @TaskTypeCode
        SELECT TOP 1 tt.TaskTypeId AS MasterDataId
        FROM dbo.tblTaskType tt WITH (NOLOCK)
        WHERE tt.IsDeleted = 0 AND tt.IsPublished = 1 AND tt.Id = @ParsedGuid;

    -- ── TaskStatus → tblTaskStatus.TaskStatusId ───────────────────────────
    ELSE IF @MasterDataCode = @TaskStatusCode
        SELECT TOP 1 ts.TaskStatusId AS MasterDataId
        FROM dbo.tblTaskStatus ts WITH (NOLOCK)
        WHERE ts.IsDeleted = 0 AND ts.IsPublished = 1 AND ts.Id = @ParsedGuid;

    -- ── AttendanceStatus → tblAttendanceStatus.AttendanceStatusId ─────────
    ELSE IF @MasterDataCode = @AttendanceStatusCode
        SELECT TOP 1 ast.AttendanceStatusId AS MasterDataId
        FROM dbo.tblAttendanceStatus ast WITH (NOLOCK)
        WHERE ast.IsDeleted = 0 AND ast.IsPublished = 1 AND ast.Id = @ParsedGuid;

    -- ── RewardType → tblRewardType.RewardTypeId ───────────────────────────
    ELSE IF @MasterDataCode = @RewardTypeCode
        SELECT TOP 1 rt.RewardTypeId AS MasterDataId
        FROM dbo.tblRewardType rt WITH (NOLOCK)
        WHERE rt.IsDeleted = 0 AND rt.IsPublished = 1 AND rt.Id = @ParsedGuid;

    -- ── CoinTransactionType → tblCoinTransactionType.CoinTransactionTypeId ─
    ELSE IF @MasterDataCode = @CoinTransactionTypeCode
        SELECT TOP 1 ct.CoinTransactionTypeId AS MasterDataId
        FROM dbo.tblCoinTransactionType ct WITH (NOLOCK)
        WHERE ct.IsDeleted = 0 AND ct.IsPublished = 1 AND ct.Id = @ParsedGuid;

    -- ── FeedbackRating → tblFeedbackRating.FeedbackRatingId ──────────────
    ELSE IF @MasterDataCode = @FeedbackRatingCode
        SELECT TOP 1 fr.FeedbackRatingId AS MasterDataId
        FROM dbo.tblFeedbackRating fr WITH (NOLOCK)
        WHERE fr.IsDeleted = 0 AND fr.IsPublished = 1 AND fr.Id = @ParsedGuid;

    -- ── CalendarEventType → tblCalendarEventType.CalendarEventTypeId ──────
    ELSE IF @MasterDataCode = @CalendarEventTypeCode
        SELECT TOP 1 cet.CalendarEventTypeId AS MasterDataId
        FROM dbo.tblCalendarEventType cet WITH (NOLOCK)
        WHERE cet.IsDeleted = 0 AND cet.IsPublished = 1 AND cet.Id = @ParsedGuid;

    -- ── NotificationType → tblNotificationType.NotificationTypeId ─────────
    ELSE IF @MasterDataCode = @NotificationTypeCode
        SELECT TOP 1 nt.NotificationTypeId AS MasterDataId
        FROM dbo.tblNotificationType nt WITH (NOLOCK)
        WHERE nt.IsDeleted = 0 AND nt.IsPublished = 1 AND nt.Id = @ParsedGuid;

    -- ── OTPType → tblOTPType.OTPTypeId ────────────────────────────────────
    ELSE IF @MasterDataCode = @OTPTypeCode
        SELECT TOP 1 ot.OTPTypeId AS MasterDataId
        FROM dbo.tblOTPType ot WITH (NOLOCK)
        WHERE ot.IsDeleted = 0 AND ot.IsPublished = 1 AND ot.Id = @ParsedGuid;

    -- Unrecognised code → no rows. BAL checks @@ROWCOUNT.
END
GO
