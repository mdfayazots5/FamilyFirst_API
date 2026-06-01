-- ============================================================
-- Script  : 097_CreateSP_FeedbackSearchCode.sql
-- Module  : Teacher Feedback (FEEDBACK — ModuleId 6)
-- SPs:
--   uspGetTeacherFeedbackBySearch
--   uspGetTeacherFeedbackById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.uspGetTeacherFeedbackBySearch
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
        fb.Id,
        fb.FeedbackType,
        fb.Severity,
        fb.Subject,
        fb.IsAcknowledged,
        fb.DateCreated,
        cp.Id           AS ChildProfileGuid,
        uc.FullName     AS ChildName,
        tp.Id           AS TeacherProfileGuid,
        ut.FullName     AS TeacherName,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblTeacherFeedback fb WITH (NOLOCK)
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = fb.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser uc WITH (NOLOCK) ON uc.UserId = cp.UserId AND uc.IsDeleted = 0
    JOIN dbo.tblTeacherProfile tp WITH (NOLOCK) ON tp.TeacherProfileId = fb.TeacherProfileId AND tp.IsDeleted = 0
    JOIN dbo.tblUser ut WITH (NOLOCK) ON ut.UserId = tp.UserId AND ut.IsDeleted = 0
    WHERE fb.IsDeleted = 0
      AND (@FamilyId = 0 OR fb.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR fb.Subject   LIKE N'%' + @SearchWord + N'%'
           OR uc.FullName  LIKE N'%' + @SearchWord + N'%'
           OR ut.FullName  LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR fb.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR fb.DateCreated <= @ToDate)
    ORDER BY fb.DateCreated DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetTeacherFeedbackById
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
        fb.Id,
        fb.FeedbackType,
        fb.Severity,
        fb.Subject,
        fb.Message,
        fb.WeeklySummaryJson,
        fb.IsAcknowledged,
        fb.AcknowledgedAt,
        fb.ParentResponseText,
        fb.DateCreated,
        fb.LastUpdated,
        cp.Id       AS ChildProfileGuid,
        uc.FullName AS ChildName,
        tp.Id       AS TeacherProfileGuid,
        ut.FullName AS TeacherName
    FROM dbo.tblTeacherFeedback fb WITH (NOLOCK)
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = fb.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser uc WITH (NOLOCK) ON uc.UserId = cp.UserId AND uc.IsDeleted = 0
    JOIN dbo.tblTeacherProfile tp WITH (NOLOCK) ON tp.TeacherProfileId = fb.TeacherProfileId AND tp.IsDeleted = 0
    JOIN dbo.tblUser ut WITH (NOLOCK) ON ut.UserId = tp.UserId AND ut.IsDeleted = 0
    WHERE fb.IsDeleted = 0
      AND (@FamilyId = 0 OR fb.FamilyId = @FamilyId)
      AND fb.Id = @ParsedId;
END
GO
