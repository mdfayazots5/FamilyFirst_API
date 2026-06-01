-- ============================================================
-- Script  : 095_CreateSP_AttendanceSearchCode.sql
-- Module  : Attendance (ATTEND — ModuleId 4)
-- SPs:
--   uspGetAttendanceSessionBySearch
--   uspGetAttendanceSessionById
--   uspGetAttendanceRecordBySearch
--   uspGetAttendanceRecordById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── AttendanceSession ─────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetAttendanceSessionBySearch
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
        s.Id,
        s.SessionName,
        s.SubjectName,
        s.BatchName,
        s.ScheduledDate,
        s.StartTime,
        s.EndTime,
        s.IsSubmitted,
        s.SubmittedAt,
        s.IsRecurring,
        tp.Id           AS TeacherProfileGuid,
        u.FullName      AS TeacherName,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblAttendanceSession s WITH (NOLOCK)
    JOIN dbo.tblTeacherProfile tp WITH (NOLOCK) ON tp.TeacherProfileId = s.TeacherProfileId AND tp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
    WHERE s.IsDeleted = 0
      AND s.IsActive  = 1
      AND (@FamilyId = 0 OR s.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR s.SessionName  LIKE N'%' + @SearchWord + N'%'
           OR s.SubjectName  LIKE N'%' + @SearchWord + N'%'
           OR u.FullName     LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR s.ScheduledDate >= @FromDate)
      AND (@ToDate   IS NULL OR s.ScheduledDate <= @ToDate)
    ORDER BY s.ScheduledDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetAttendanceSessionById
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
        s.Id,
        s.SessionName,
        s.SubjectName,
        s.BatchName,
        s.ScheduledDate,
        s.StartTime,
        s.EndTime,
        s.IsSubmitted,
        s.SubmittedAt,
        s.IsRecurring,
        s.RecurringDays,
        tp.Id       AS TeacherProfileGuid,
        u.FullName  AS TeacherName,
        s.DateCreated,
        s.LastUpdated
    FROM dbo.tblAttendanceSession s WITH (NOLOCK)
    JOIN dbo.tblTeacherProfile tp WITH (NOLOCK) ON tp.TeacherProfileId = s.TeacherProfileId AND tp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
    WHERE s.IsDeleted = 0
      AND (@FamilyId = 0 OR s.FamilyId = @FamilyId)
      AND s.Id = @ParsedId;
END
GO

-- ── AttendanceRecord ──────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetAttendanceRecordBySearch
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
        ar.Id,
        s.Id            AS SessionGuid,
        s.SessionName,
        s.ScheduledDate,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        ar.Status,
        ar.TeacherComment,
        ar.MarkedAt,
        ar.EditedAt,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblAttendanceRecord ar WITH (NOLOCK)
    JOIN dbo.tblAttendanceSession s WITH (NOLOCK) ON s.AttendanceSessionId = ar.AttendanceSessionId AND s.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ar.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE ar.IsDeleted = 0
      AND (@FamilyId = 0 OR ar.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR u.FullName    LIKE N'%' + @SearchWord + N'%'
           OR s.SessionName LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR s.ScheduledDate >= @FromDate)
      AND (@ToDate   IS NULL OR s.ScheduledDate <= @ToDate)
    ORDER BY s.ScheduledDate DESC, u.FullName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetAttendanceRecordById
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
        ar.Id,
        s.Id            AS SessionGuid,
        s.SessionName,
        s.ScheduledDate,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        ar.Status,
        ar.TeacherComment,
        ar.CommentTemplateId,
        ar.MarkedAt,
        ar.EditedAt,
        ar.LastUpdated
    FROM dbo.tblAttendanceRecord ar WITH (NOLOCK)
    JOIN dbo.tblAttendanceSession s WITH (NOLOCK) ON s.AttendanceSessionId = ar.AttendanceSessionId AND s.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ar.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE ar.IsDeleted = 0
      AND (@FamilyId = 0 OR ar.FamilyId = @FamilyId)
      AND ar.Id = @ParsedId;
END
GO
