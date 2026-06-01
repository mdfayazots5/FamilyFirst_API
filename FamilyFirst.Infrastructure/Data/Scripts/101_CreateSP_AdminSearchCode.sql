-- ============================================================
-- Script  : 101_CreateSP_AdminSearchCode.sql
-- Module  : Admin Configuration (ADMIN — ModuleId 10)
-- SPs:
--   uspGetFeatureFlagBySearch
--   uspGetFeatureFlagById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.uspGetFeatureFlagBySearch
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
        ff.Id,
        ff.FlagKey,
        ff.FlagValue,
        ff.Description,
        ff.IsPublished,
        ff.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblFeatureFlag ff WITH (NOLOCK)
    WHERE ff.IsDeleted = 0
      AND (@SearchWord IS NULL
           OR ff.FlagKey     LIKE N'%' + @SearchWord + N'%'
           OR ff.Description LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR ff.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR ff.DateCreated <= @ToDate)
    ORDER BY ff.FlagKey
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetFeatureFlagById
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
        ff.Id,
        ff.FlagKey,
        ff.FlagValue,
        ff.Description,
        ff.IsPublished,
        ff.DateCreated,
        ff.LastUpdated
    FROM dbo.tblFeatureFlag ff WITH (NOLOCK)
    WHERE ff.IsDeleted = 0
      AND ff.Id = @ParsedId;
END
GO
