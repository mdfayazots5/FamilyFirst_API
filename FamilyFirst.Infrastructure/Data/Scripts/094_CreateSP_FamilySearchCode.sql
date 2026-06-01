-- ============================================================
-- Script  : 094_CreateSP_FamilySearchCode.sql
-- Module  : Family Management (FAMILY — ModuleId 2)
-- Purpose : Search and Code SPs for Family, FamilyMember,
--           ChildProfile, TeacherProfile entities.
--           Called exclusively via the generic GetDataBySearch /
--           GetDataByCode API — NOT through individual GET endpoints.
--
-- Standard parameter interface (all SPs share this — generic API
-- passes the full set; each SP uses only what it needs):
--   @FamilyId, @UserId, @RoleId, @Id, @SearchWord,
--   @FromDate, @ToDate, @PageNumber, @PageSize, @LanguageId
-- ============================================================
-- SP List:
--   uspGetFamilyBySearch          FAMILY / GetDataBySearch
--   uspGetFamilyById              FAMILY / GetDataByCode
--   uspGetFamilyMemberBySearch    FAMILY / GetFamilyMemberBySearch
--   uspGetFamilyMemberById        FAMILY / GetFamilyMemberById
--   uspGetChildProfileBySearch    FAMILY / GetChildProfileBySearch
--   uspGetChildProfileById        FAMILY / GetChildProfileById
--   uspGetTeacherProfileBySearch  FAMILY / GetTeacherProfileBySearch
--   uspGetTeacherProfileById      FAMILY / GetTeacherProfileById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── Family ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetFamilyBySearch
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
        f.Id,
        f.FamilyName,
        f.JoinCode,
        f.City,
        f.IsActive,
        f.FamilyScore,
        f.CurrentStreakDays,
        f.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblFamily f WITH (NOLOCK)
    WHERE f.IsDeleted = 0
      AND (@FamilyId = 0 OR f.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL OR f.FamilyName LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR f.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR f.DateCreated <= @ToDate)
    ORDER BY f.DateCreated DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetFamilyById
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
        f.Id,
        f.FamilyName,
        f.JoinCode,
        f.City,
        f.IsActive,
        f.FamilyScore,
        f.CurrentStreakDays,
        f.BestStreakDays,
        f.TimezoneId,
        f.FamilyAdminUserId,
        f.DateCreated,
        f.LastUpdated
    FROM dbo.tblFamily f WITH (NOLOCK)
    WHERE f.IsDeleted = 0
      AND (@FamilyId = 0 OR f.FamilyId = @FamilyId)
      AND f.Id = @ParsedId;
END
GO

-- ── FamilyMember ──────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetFamilyMemberBySearch
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
        fm.Id,
        ISNULL(fm.DisplayName, u.FullName) AS MemberName,
        u.PhoneNumber,
        fm.Role,
        fm.LinkType,
        fm.IsActive,
        fm.JoinedAt,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblFamilyMember fm WITH (NOLOCK)
    LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = fm.UserId AND u.IsDeleted = 0
    WHERE fm.IsDeleted = 0
      AND (@FamilyId = 0 OR fm.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR ISNULL(fm.DisplayName, u.FullName) LIKE N'%' + @SearchWord + N'%'
           OR u.PhoneNumber LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR fm.JoinedAt >= @FromDate)
      AND (@ToDate   IS NULL OR fm.JoinedAt <= @ToDate)
    ORDER BY fm.JoinedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetFamilyMemberById
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
        fm.Id,
        ISNULL(fm.DisplayName, u.FullName) AS MemberName,
        u.Id                               AS UserGuid,
        u.FullName,
        u.PhoneNumber,
        u.Email,
        u.ProfilePhotoUrl,
        fm.Role,
        fm.LinkType,
        fm.IsActive,
        fm.JoinedAt
    FROM dbo.tblFamilyMember fm WITH (NOLOCK)
    LEFT JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = fm.UserId AND u.IsDeleted = 0
    WHERE fm.IsDeleted = 0
      AND (@FamilyId = 0 OR fm.FamilyId = @FamilyId)
      AND fm.Id = @ParsedId;
END
GO

-- ── ChildProfile ──────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetChildProfileBySearch
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
        cp.Id,
        u.FullName          AS ChildName,
        cp.GradeLevel,
        cp.SchoolName,
        cp.AvatarCode,
        cp.CoinBalance,
        cp.CurrentStreakDays,
        cp.LevelCode,
        cp.DateCreated,
        COUNT(1) OVER()     AS TotalCount
    FROM dbo.tblChildProfile cp WITH (NOLOCK)
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE cp.IsDeleted = 0
      AND (@FamilyId = 0 OR cp.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR u.FullName    LIKE N'%' + @SearchWord + N'%'
           OR cp.SchoolName LIKE N'%' + @SearchWord + N'%')
    ORDER BY u.FullName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetChildProfileById
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
        cp.Id,
        u.Id            AS UserGuid,
        u.FullName      AS ChildName,
        cp.DateOfBirth,
        cp.GradeLevel,
        cp.SchoolName,
        cp.AvatarCode,
        cp.CoinBalance,
        cp.TotalCoinsEarned,
        cp.CurrentStreakDays,
        cp.BestStreakDays,
        cp.LevelCode,
        cp.StudyScore,
        cp.DateCreated
    FROM dbo.tblChildProfile cp WITH (NOLOCK)
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE cp.IsDeleted = 0
      AND (@FamilyId = 0 OR cp.FamilyId = @FamilyId)
      AND cp.Id = @ParsedId;
END
GO

-- ── TeacherProfile ────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetTeacherProfileBySearch
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
        tp.Id,
        u.FullName      AS TeacherName,
        tp.SubjectName,
        tp.TeacherType,
        tp.IsActive,
        tp.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblTeacherProfile tp WITH (NOLOCK)
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
    WHERE tp.IsDeleted = 0
      AND tp.IsActive  = 1
      AND (@FamilyId = 0 OR tp.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR u.FullName     LIKE N'%' + @SearchWord + N'%'
           OR tp.SubjectName LIKE N'%' + @SearchWord + N'%')
    ORDER BY u.FullName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetTeacherProfileById
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
        tp.Id,
        u.Id            AS UserGuid,
        u.FullName      AS TeacherName,
        u.PhoneNumber,
        u.ProfilePhotoUrl,
        tp.SubjectName,
        tp.TeacherType,
        tp.IsActive,
        tp.DateCreated
    FROM dbo.tblTeacherProfile tp WITH (NOLOCK)
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = tp.UserId AND u.IsDeleted = 0
    WHERE tp.IsDeleted = 0
      AND (@FamilyId = 0 OR tp.FamilyId = @FamilyId)
      AND tp.Id = @ParsedId;
END
GO
