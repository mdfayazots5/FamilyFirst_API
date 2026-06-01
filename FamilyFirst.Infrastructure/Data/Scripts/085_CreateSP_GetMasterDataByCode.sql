SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : Returns master data values for a given MasterDataCode.
--                    UI sends MasterDataCode → SP routes to the dedicated table → returns rows.
--                    Returns GUID (Id) and display name only — NEVER the INT PK.
--
-- Every MasterDataCode routes to its own dedicated table (reference pattern).
-- No inline/fallback values. If the code is unrecognised → empty result.
-- BAL interprets empty result as error 23 (Invalid_MasterData).
--
-- For each code, two branches:
--   @Code IS NULL     → return all active rows / apply @SearchWord filter
--   @Code IS NOT NULL → return single row by GUID (current value display)
--
-- FamilyId-scoped codes (FamilyMember, ChildProfile, TeacherProfile,
--   CustomAttendanceStatus, Reward) filter by @FamilyId when non-zero.
--
-- NO hardcoded error messages or return codes — see Section 6B of New SQL Format.txt.
--
-- Usage : EXEC dbo.uspGetMasterDataByCode @MasterDataCode = 'TaskType'
--         EXEC dbo.uspGetMasterDataByCode @MasterDataCode = 'ChildProfile', @FamilyId = 42
--         EXEC dbo.uspGetMasterDataByCode @MasterDataCode = 'AttendanceStatus', @Code = 'A1B2...'
--         EXEC dbo.uspGetMasterDataByCode @MasterDataCode = 'Role', @SearchWord = 'Admin'
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation — all FamilyFirst L1 tables
-- 1.1       Claude Project AI Engineer 01 Jun 2026    All codes route to dedicated tables
-- 1.2       Claude Project AI Engineer 01 Jun 2026    Removed inline fallback — dedicated tables only
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspGetMasterDataByCode
(
    @MasterDataCode     NVARCHAR(64)    = NULL,
    @Code               NVARCHAR(64)    = NULL,
    @SearchWord         NVARCHAR(256)   = NULL,
    @IsPublished        BIT             = 1,
    @LanguageId         INT             = 1,
    @FamilyId           BIGINT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ParsedCode UNIQUEIDENTIFIER = TRY_CAST(@Code AS UNIQUEIDENTIFIER);

    -- Code constants — each maps to a dedicated table
    DECLARE @FamilyCode                 NVARCHAR(64) = N'Family';
    DECLARE @RoleCode                   NVARCHAR(64) = N'Role';
    DECLARE @ModuleCode                 NVARCHAR(64) = N'Module';
    DECLARE @PermissionCode             NVARCHAR(64) = N'Permission';
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

    -- ── Family → tblFamily ───────────────────────────────────────────────
    IF @MasterDataCode = @FamilyCode
    BEGIN
        IF @Code IS NULL
            SELECT f.Id, f.FamilyName AS [Name], f.JoinCode AS [Code], 0 AS SortOrder
            FROM dbo.tblFamily f WITH (NOLOCK)
            WHERE f.IsDeleted = 0 AND f.IsActive = 1
              AND (@SearchWord IS NULL OR f.FamilyName LIKE N'%' + @SearchWord + N'%')
            ORDER BY f.FamilyName;
        ELSE
            SELECT TOP 1 f.Id, f.FamilyName AS [Name], f.JoinCode AS [Code], 0 AS SortOrder
            FROM dbo.tblFamily f WITH (NOLOCK)
            WHERE f.IsDeleted = 0 AND f.IsActive = 1 AND f.Id = @ParsedCode;
    END

    -- ── Role → tblRole ────────────────────────────────────────────────────
    IF @MasterDataCode = @RoleCode
    BEGIN
        IF @Code IS NULL
            SELECT r.Id, r.RoleName AS [Name], r.RoleCode AS [Code], r.SortOrder
            FROM dbo.tblRole r WITH (NOLOCK)
            WHERE r.IsDeleted = 0 AND r.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR r.RoleName LIKE N'%' + @SearchWord + N'%')
            ORDER BY r.SortOrder;
        ELSE
            SELECT TOP 1 r.Id, r.RoleName AS [Name], r.RoleCode AS [Code], r.SortOrder
            FROM dbo.tblRole r WITH (NOLOCK)
            WHERE r.IsDeleted = 0 AND r.IsPublished = @IsPublished AND r.Id = @ParsedCode;
    END

    -- ── Module → tblModule ────────────────────────────────────────────────
    ELSE IF @MasterDataCode = @ModuleCode
    BEGIN
        IF @Code IS NULL
            SELECT m.Id, m.ModuleName AS [Name], m.ModuleCode AS [Code], m.SortOrder
            FROM dbo.tblModule m WITH (NOLOCK)
            WHERE m.IsDeleted = 0 AND m.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR m.ModuleName LIKE N'%' + @SearchWord + N'%')
            ORDER BY m.SortOrder;
        ELSE
            SELECT TOP 1 m.Id, m.ModuleName AS [Name], m.ModuleCode AS [Code], m.SortOrder
            FROM dbo.tblModule m WITH (NOLOCK)
            WHERE m.IsDeleted = 0 AND m.IsPublished = @IsPublished AND m.Id = @ParsedCode;
    END

    -- ── Permission → tblPermission ────────────────────────────────────────
    ELSE IF @MasterDataCode = @PermissionCode
    BEGIN
        IF @Code IS NULL
            SELECT p.Id, p.PermissionName AS [Name], p.PermissionCode AS [Code], p.SortOrder
            FROM dbo.tblPermission p WITH (NOLOCK)
            WHERE p.IsDeleted = 0 AND p.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR p.PermissionName LIKE N'%' + @SearchWord + N'%')
            ORDER BY p.SortOrder;
        ELSE
            SELECT TOP 1 p.Id, p.PermissionName AS [Name], p.PermissionCode AS [Code], p.SortOrder
            FROM dbo.tblPermission p WITH (NOLOCK)
            WHERE p.IsDeleted = 0 AND p.IsPublished = @IsPublished AND p.Id = @ParsedCode;
    END

    -- ── Plan → tblPlan ────────────────────────────────────────────────────
    ELSE IF @MasterDataCode = @PlanCode
    BEGIN
        IF @Code IS NULL
            SELECT pl.Id, pl.PlanName AS [Name], pl.PlanCode AS [Code], pl.SortOrder
            FROM dbo.tblPlan pl WITH (NOLOCK)
            WHERE pl.IsDeleted = 0 AND pl.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR pl.PlanName LIKE N'%' + @SearchWord + N'%')
            ORDER BY pl.SortOrder;
        ELSE
            SELECT TOP 1 pl.Id, pl.PlanName AS [Name], pl.PlanCode AS [Code], pl.SortOrder
            FROM dbo.tblPlan pl WITH (NOLOCK)
            WHERE pl.IsDeleted = 0 AND pl.IsPublished = @IsPublished AND pl.Id = @ParsedCode;
    END

    -- ── User → tblUser ────────────────────────────────────────────────────
    ELSE IF @MasterDataCode = @UserCode
    BEGIN
        IF @Code IS NULL
            SELECT u.Id, u.FullName AS [Name], u.PhoneNumber AS [Code], 0 AS SortOrder
            FROM dbo.tblUser u WITH (NOLOCK)
            WHERE u.IsDeleted = 0 AND u.IsActive = 1
              AND (@SearchWord IS NULL
                   OR u.FullName    LIKE N'%' + @SearchWord + N'%'
                   OR u.PhoneNumber LIKE N'%' + @SearchWord + N'%')
            ORDER BY u.FullName;
        ELSE
            SELECT TOP 1 u.Id, u.FullName AS [Name], u.PhoneNumber AS [Code], 0 AS SortOrder
            FROM dbo.tblUser u WITH (NOLOCK)
            WHERE u.IsDeleted = 0 AND u.IsActive = 1 AND u.Id = @ParsedCode;
    END

    -- ── FamilyMember → tblFamilyMember [FamilyId scoped] ─────────────────
    ELSE IF @MasterDataCode = @FamilyMemberCode
    BEGIN
        IF @Code IS NULL
            SELECT fm.Id,
                   ISNULL(fm.DisplayName, u.FullName)  AS [Name],
                   CAST(fm.Role AS NVARCHAR(8))         AS [Code],
                   0                                    AS SortOrder
            FROM dbo.tblFamilyMember fm WITH (NOLOCK)
            LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = fm.UserId AND u.IsDeleted = 0
            WHERE fm.IsDeleted = 0 AND fm.IsActive = 1
              AND (@FamilyId = 0 OR fm.FamilyId = @FamilyId)
              AND (@SearchWord IS NULL
                   OR ISNULL(fm.DisplayName, u.FullName) LIKE N'%' + @SearchWord + N'%')
            ORDER BY ISNULL(fm.DisplayName, u.FullName);
        ELSE
            SELECT TOP 1
                   fm.Id,
                   ISNULL(fm.DisplayName, u.FullName)  AS [Name],
                   CAST(fm.Role AS NVARCHAR(8))         AS [Code],
                   0                                    AS SortOrder
            FROM dbo.tblFamilyMember fm WITH (NOLOCK)
            LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = fm.UserId AND u.IsDeleted = 0
            WHERE fm.IsDeleted = 0 AND fm.IsActive = 1
              AND (@FamilyId = 0 OR fm.FamilyId = @FamilyId)
              AND fm.Id = @ParsedCode;
    END

    -- ── ChildProfile → tblChildProfile [FamilyId scoped] ─────────────────
    ELSE IF @MasterDataCode = @ChildProfileCode
    BEGIN
        IF @Code IS NULL
            SELECT cp.Id, u.FullName AS [Name], ISNULL(cp.GradeLevel, N'') AS [Code], 0 AS SortOrder
            FROM dbo.tblChildProfile cp WITH (NOLOCK)
            JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
            WHERE cp.IsDeleted = 0
              AND (@FamilyId = 0 OR cp.FamilyId = @FamilyId)
              AND (@SearchWord IS NULL
                   OR u.FullName      LIKE N'%' + @SearchWord + N'%'
                   OR cp.GradeLevel  LIKE N'%' + @SearchWord + N'%'
                   OR cp.SchoolName  LIKE N'%' + @SearchWord + N'%')
            ORDER BY u.FullName;
        ELSE
            SELECT TOP 1 cp.Id, u.FullName AS [Name], ISNULL(cp.GradeLevel, N'') AS [Code], 0 AS SortOrder
            FROM dbo.tblChildProfile cp WITH (NOLOCK)
            JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
            WHERE cp.IsDeleted = 0
              AND (@FamilyId = 0 OR cp.FamilyId = @FamilyId)
              AND cp.Id = @ParsedCode;
    END

    -- ── TeacherProfile → tblTeacherProfile [FamilyId scoped] ─────────────
    ELSE IF @MasterDataCode = @TeacherProfileCode
    BEGIN
        IF @Code IS NULL
            SELECT tp.Id, u.FullName AS [Name], tp.SubjectName AS [Code], 0 AS SortOrder
            FROM dbo.tblTeacherProfile tp WITH (NOLOCK)
            JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
            WHERE tp.IsDeleted = 0 AND tp.IsActive = 1
              AND (@FamilyId = 0 OR tp.FamilyId = @FamilyId)
              AND (@SearchWord IS NULL
                   OR u.FullName      LIKE N'%' + @SearchWord + N'%'
                   OR tp.SubjectName LIKE N'%' + @SearchWord + N'%')
            ORDER BY u.FullName;
        ELSE
            SELECT TOP 1 tp.Id, u.FullName AS [Name], tp.SubjectName AS [Code], 0 AS SortOrder
            FROM dbo.tblTeacherProfile tp WITH (NOLOCK)
            JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
            WHERE tp.IsDeleted = 0 AND tp.IsActive = 1
              AND (@FamilyId = 0 OR tp.FamilyId = @FamilyId)
              AND tp.Id = @ParsedCode;
    END

    -- ── CustomAttendanceStatus → tblCustomAttendanceStatuses [FamilyId] ───
    ELSE IF @MasterDataCode = @CustomAttendanceStatusCode
    BEGIN
        IF @Code IS NULL
            SELECT cas.Id, cas.StatusName AS [Name], cas.ColorHex AS [Code], cas.SortOrder
            FROM dbo.tblCustomAttendanceStatuses cas WITH (NOLOCK)
            WHERE cas.IsDeleted = 0
              AND (@FamilyId = 0 OR cas.FamilyId = @FamilyId)
              AND (@SearchWord IS NULL OR cas.StatusName LIKE N'%' + @SearchWord + N'%')
            ORDER BY cas.SortOrder, cas.StatusName;
        ELSE
            SELECT TOP 1 cas.Id, cas.StatusName AS [Name], cas.ColorHex AS [Code], cas.SortOrder
            FROM dbo.tblCustomAttendanceStatuses cas WITH (NOLOCK)
            WHERE cas.IsDeleted = 0
              AND (@FamilyId = 0 OR cas.FamilyId = @FamilyId)
              AND cas.Id = @ParsedCode;
    END

    -- ── Reward → tblReward [FamilyId scoped + system rewards] ─────────────
    ELSE IF @MasterDataCode = @RewardCode
    BEGIN
        IF @Code IS NULL
            SELECT r.Id, r.RewardName AS [Name], CAST(r.CoinCost AS NVARCHAR(16)) AS [Code], r.SortOrder
            FROM dbo.tblReward r WITH (NOLOCK)
            WHERE r.IsDeleted = 0 AND r.IsPublished = @IsPublished
              AND (@FamilyId = 0 OR r.FamilyId = @FamilyId OR r.FamilyId IS NULL)
              AND (@SearchWord IS NULL OR r.RewardName LIKE N'%' + @SearchWord + N'%')
            ORDER BY r.SortOrder, r.RewardName;
        ELSE
            SELECT TOP 1 r.Id, r.RewardName AS [Name], CAST(r.CoinCost AS NVARCHAR(16)) AS [Code], r.SortOrder
            FROM dbo.tblReward r WITH (NOLOCK)
            WHERE r.IsDeleted = 0 AND r.IsPublished = @IsPublished
              AND (@FamilyId = 0 OR r.FamilyId = @FamilyId OR r.FamilyId IS NULL)
              AND r.Id = @ParsedCode;
    END

    -- ── TaskType → tblTaskType ────────────────────────────────────────────
    ELSE IF @MasterDataCode = @TaskTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT tt.Id, tt.TaskTypeName AS [Name], tt.TaskTypeCode AS [Code], tt.SortOrder
            FROM dbo.tblTaskType tt WITH (NOLOCK)
            WHERE tt.IsDeleted = 0 AND tt.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR tt.TaskTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY tt.SortOrder;
        ELSE
            SELECT TOP 1 tt.Id, tt.TaskTypeName AS [Name], tt.TaskTypeCode AS [Code], tt.SortOrder
            FROM dbo.tblTaskType tt WITH (NOLOCK)
            WHERE tt.IsDeleted = 0 AND tt.IsPublished = @IsPublished AND tt.Id = @ParsedCode;
    END

    -- ── TaskStatus → tblTaskStatus ────────────────────────────────────────
    ELSE IF @MasterDataCode = @TaskStatusCode
    BEGIN
        IF @Code IS NULL
            SELECT ts.Id, ts.TaskStatusName AS [Name], ts.TaskStatusCode AS [Code], ts.SortOrder
            FROM dbo.tblTaskStatus ts WITH (NOLOCK)
            WHERE ts.IsDeleted = 0 AND ts.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR ts.TaskStatusName LIKE N'%' + @SearchWord + N'%')
            ORDER BY ts.SortOrder;
        ELSE
            SELECT TOP 1 ts.Id, ts.TaskStatusName AS [Name], ts.TaskStatusCode AS [Code], ts.SortOrder
            FROM dbo.tblTaskStatus ts WITH (NOLOCK)
            WHERE ts.IsDeleted = 0 AND ts.IsPublished = @IsPublished AND ts.Id = @ParsedCode;
    END

    -- ── AttendanceStatus → tblAttendanceStatus ────────────────────────────
    ELSE IF @MasterDataCode = @AttendanceStatusCode
    BEGIN
        IF @Code IS NULL
            SELECT ast.Id, ast.AttendanceStatusName AS [Name], ast.AttendanceStatusCode AS [Code], ast.SortOrder
            FROM dbo.tblAttendanceStatus ast WITH (NOLOCK)
            WHERE ast.IsDeleted = 0 AND ast.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR ast.AttendanceStatusName LIKE N'%' + @SearchWord + N'%')
            ORDER BY ast.SortOrder;
        ELSE
            SELECT TOP 1 ast.Id, ast.AttendanceStatusName AS [Name], ast.AttendanceStatusCode AS [Code], ast.SortOrder
            FROM dbo.tblAttendanceStatus ast WITH (NOLOCK)
            WHERE ast.IsDeleted = 0 AND ast.IsPublished = @IsPublished AND ast.Id = @ParsedCode;
    END

    -- ── RewardType → tblRewardType ────────────────────────────────────────
    ELSE IF @MasterDataCode = @RewardTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT rt.Id, rt.RewardTypeName AS [Name], rt.RewardTypeCode AS [Code], rt.SortOrder
            FROM dbo.tblRewardType rt WITH (NOLOCK)
            WHERE rt.IsDeleted = 0 AND rt.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR rt.RewardTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY rt.SortOrder;
        ELSE
            SELECT TOP 1 rt.Id, rt.RewardTypeName AS [Name], rt.RewardTypeCode AS [Code], rt.SortOrder
            FROM dbo.tblRewardType rt WITH (NOLOCK)
            WHERE rt.IsDeleted = 0 AND rt.IsPublished = @IsPublished AND rt.Id = @ParsedCode;
    END

    -- ── CoinTransactionType → tblCoinTransactionType ──────────────────────
    ELSE IF @MasterDataCode = @CoinTransactionTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT ct.Id, ct.CoinTransactionTypeName AS [Name], ct.CoinTransactionTypeCode AS [Code], ct.SortOrder
            FROM dbo.tblCoinTransactionType ct WITH (NOLOCK)
            WHERE ct.IsDeleted = 0 AND ct.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR ct.CoinTransactionTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY ct.SortOrder;
        ELSE
            SELECT TOP 1 ct.Id, ct.CoinTransactionTypeName AS [Name], ct.CoinTransactionTypeCode AS [Code], ct.SortOrder
            FROM dbo.tblCoinTransactionType ct WITH (NOLOCK)
            WHERE ct.IsDeleted = 0 AND ct.IsPublished = @IsPublished AND ct.Id = @ParsedCode;
    END

    -- ── FeedbackRating → tblFeedbackRating ───────────────────────────────
    ELSE IF @MasterDataCode = @FeedbackRatingCode
    BEGIN
        IF @Code IS NULL
            SELECT fr.Id, fr.FeedbackRatingName AS [Name], fr.FeedbackRatingCode AS [Code], fr.SortOrder
            FROM dbo.tblFeedbackRating fr WITH (NOLOCK)
            WHERE fr.IsDeleted = 0 AND fr.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR fr.FeedbackRatingName LIKE N'%' + @SearchWord + N'%')
            ORDER BY fr.SortOrder;
        ELSE
            SELECT TOP 1 fr.Id, fr.FeedbackRatingName AS [Name], fr.FeedbackRatingCode AS [Code], fr.SortOrder
            FROM dbo.tblFeedbackRating fr WITH (NOLOCK)
            WHERE fr.IsDeleted = 0 AND fr.IsPublished = @IsPublished AND fr.Id = @ParsedCode;
    END

    -- ── CalendarEventType → tblCalendarEventType ──────────────────────────
    ELSE IF @MasterDataCode = @CalendarEventTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT cet.Id, cet.CalendarEventTypeName AS [Name], cet.CalendarEventTypeCode AS [Code], cet.SortOrder
            FROM dbo.tblCalendarEventType cet WITH (NOLOCK)
            WHERE cet.IsDeleted = 0 AND cet.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR cet.CalendarEventTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY cet.SortOrder;
        ELSE
            SELECT TOP 1 cet.Id, cet.CalendarEventTypeName AS [Name], cet.CalendarEventTypeCode AS [Code], cet.SortOrder
            FROM dbo.tblCalendarEventType cet WITH (NOLOCK)
            WHERE cet.IsDeleted = 0 AND cet.IsPublished = @IsPublished AND cet.Id = @ParsedCode;
    END

    -- ── NotificationType → tblNotificationType ────────────────────────────
    ELSE IF @MasterDataCode = @NotificationTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT nt.Id, nt.NotificationTypeName AS [Name], nt.NotificationTypeCode AS [Code], nt.SortOrder
            FROM dbo.tblNotificationType nt WITH (NOLOCK)
            WHERE nt.IsDeleted = 0 AND nt.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR nt.NotificationTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY nt.SortOrder;
        ELSE
            SELECT TOP 1 nt.Id, nt.NotificationTypeName AS [Name], nt.NotificationTypeCode AS [Code], nt.SortOrder
            FROM dbo.tblNotificationType nt WITH (NOLOCK)
            WHERE nt.IsDeleted = 0 AND nt.IsPublished = @IsPublished AND nt.Id = @ParsedCode;
    END

    -- ── OTPType → tblOTPType ──────────────────────────────────────────────
    ELSE IF @MasterDataCode = @OTPTypeCode
    BEGIN
        IF @Code IS NULL
            SELECT ot.Id, ot.OTPTypeName AS [Name], ot.OTPTypeCode AS [Code], ot.SortOrder
            FROM dbo.tblOTPType ot WITH (NOLOCK)
            WHERE ot.IsDeleted = 0 AND ot.IsPublished = @IsPublished
              AND (@SearchWord IS NULL OR ot.OTPTypeName LIKE N'%' + @SearchWord + N'%')
            ORDER BY ot.SortOrder;
        ELSE
            SELECT TOP 1 ot.Id, ot.OTPTypeName AS [Name], ot.OTPTypeCode AS [Code], ot.SortOrder
            FROM dbo.tblOTPType ot WITH (NOLOCK)
            WHERE ot.IsDeleted = 0 AND ot.IsPublished = @IsPublished AND ot.Id = @ParsedCode;
    END

    -- Unrecognised MasterDataCode → no rows returned.
    -- BAL must check @@ROWCOUNT after calling this SP.
END
GO
