-- ============================================================
-- Script  : 096_CreateSP_TaskSearchCode.sql
-- Module  : Tasks (TASK — ModuleId 5)
-- SPs:
--   uspGetTaskItemBySearch
--   uspGetTaskItemById
--   uspGetTaskCompletionBySearch
--   uspGetTaskCompletionById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── TaskItem ──────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetTaskItemBySearch
(
    @FamilyId   BIGINT          = 0,
    @UserId     BIGINT          = 0,
    @RoleId     INT             = 0,
    @Id         NVARCHAR(64)    = NULL,
    @SearchWord NVARCHAR(256)   = NULL,
    @FromDate   DATETIME2       = NULL,
    @ToDate     DATETIME2       = NULL,
    @PageNumber INT             = 1,
    @PageSize   INT             = 10,
    @LanguageId INT             = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ti.Id,
        ti.TaskName,
        ti.IconCode,
        ti.TimeBlock,
        ti.DurationMinutes,
        ti.CoinValue,
        ti.IsPhotoRequired,
        ti.PillarTag,
        ti.IsRecurring,
        ti.ActiveFromDate,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblTaskItem ti WITH (NOLOCK)
    LEFT JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ti.ChildProfileId AND cp.IsDeleted = 0
    LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE ti.IsDeleted = 0
      AND ti.IsPublished = 1
      AND (@FamilyId = 0 OR ti.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR ti.TaskName  LIKE N'%' + @SearchWord + N'%'
           OR ti.PillarTag LIKE N'%' + @SearchWord + N'%'
           OR u.FullName   LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR ti.ActiveFromDate >= @FromDate)
      AND (@ToDate   IS NULL OR ti.ActiveFromDate <= @ToDate)
    ORDER BY ti.DateCreated DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetTaskItemById
(
    @FamilyId   BIGINT          = 0,
    @UserId     BIGINT          = 0,
    @RoleId     INT             = 0,
    @Id         NVARCHAR(64)    = NULL,
    @SearchWord NVARCHAR(256)   = NULL,
    @FromDate   DATETIME2       = NULL,
    @ToDate     DATETIME2       = NULL,
    @PageNumber INT             = 1,
    @PageSize   INT             = 10,
    @LanguageId INT             = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ParsedId UNIQUEIDENTIFIER = TRY_CAST(@Id AS UNIQUEIDENTIFIER);
    SELECT TOP 1
        ti.Id,
        ti.TaskName,
        ti.Instructions,
        ti.IconCode,
        ti.TimeBlock,
        ti.DurationMinutes,
        ti.CoinValue,
        ti.IsPhotoRequired,
        ti.PillarTag,
        ti.IsRecurring,
        ti.RecurringDays,
        ti.ActiveFromDate,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        ti.DateCreated,
        ti.LastUpdated
    FROM dbo.tblTaskItem ti WITH (NOLOCK)
    LEFT JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ti.ChildProfileId AND cp.IsDeleted = 0
    LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE ti.IsDeleted = 0
      AND (@FamilyId = 0 OR ti.FamilyId = @FamilyId)
      AND ti.Id = @ParsedId;
END
GO

-- ── TaskCompletion ────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetTaskCompletionBySearch
(
    @FamilyId   BIGINT          = 0,
    @UserId     BIGINT          = 0,
    @RoleId     INT             = 0,
    @Id         NVARCHAR(64)    = NULL,
    @SearchWord NVARCHAR(256)   = NULL,
    @FromDate   DATETIME2       = NULL,
    @ToDate     DATETIME2       = NULL,
    @PageNumber INT             = 1,
    @PageSize   INT             = 10,
    @LanguageId INT             = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        tc.Id,
        ti.Id           AS TaskItemGuid,
        ti.TaskName,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        tc.ScheduledDate,
        tc.Status,
        tc.PhotoUrl,
        tc.SubmittedAt,
        tc.ReviewedAt,
        tc.CoinsAwarded,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblTaskCompletion tc WITH (NOLOCK)
    JOIN dbo.tblTaskItem ti WITH (NOLOCK) ON ti.TaskItemId = tc.TaskItemId AND ti.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = tc.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE tc.IsDeleted = 0
      AND (@FamilyId = 0 OR tc.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR ti.TaskName LIKE N'%' + @SearchWord + N'%'
           OR u.FullName  LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR tc.ScheduledDate >= @FromDate)
      AND (@ToDate   IS NULL OR tc.ScheduledDate <= @ToDate)
    ORDER BY tc.ScheduledDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetTaskCompletionById
(
    @FamilyId   BIGINT          = 0,
    @UserId     BIGINT          = 0,
    @RoleId     INT             = 0,
    @Id         NVARCHAR(64)    = NULL,
    @SearchWord NVARCHAR(256)   = NULL,
    @FromDate   DATETIME2       = NULL,
    @ToDate     DATETIME2       = NULL,
    @PageNumber INT             = 1,
    @PageSize   INT             = 10,
    @LanguageId INT             = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ParsedId UNIQUEIDENTIFIER = TRY_CAST(@Id AS UNIQUEIDENTIFIER);
    SELECT TOP 1
        tc.Id,
        ti.Id           AS TaskItemGuid,
        ti.TaskName,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        tc.ScheduledDate,
        tc.Status,
        tc.PhotoUrl,
        tc.SubmittedAt,
        tc.ReviewedAt,
        tc.ReviewNote,
        tc.CoinsAwarded
    FROM dbo.tblTaskCompletion tc WITH (NOLOCK)
    JOIN dbo.tblTaskItem ti WITH (NOLOCK) ON ti.TaskItemId = tc.TaskItemId AND ti.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = tc.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE tc.IsDeleted = 0
      AND (@FamilyId = 0 OR tc.FamilyId = @FamilyId)
      AND tc.Id = @ParsedId;
END
GO
