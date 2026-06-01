-- ============================================================
-- Script  : 099_CreateSP_CalendarSearchCode.sql
-- Module  : Family Calendar (CALENDAR — ModuleId 8)
-- SPs:
--   uspGetCalendarEventBySearch
--   uspGetCalendarEventById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.uspGetCalendarEventBySearch
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
        ce.Id,
        ce.EventTitle,
        ce.EventType,
        ce.StartDateTime,
        ce.EndDateTime,
        ce.IsAllDay,
        ce.Location,
        ce.ColorHex,
        ce.IsRecurring,
        ce.VisibilityScope,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblCalendarEvent ce WITH (NOLOCK)
    WHERE ce.IsDeleted = 0
      AND ce.IsPublished = 1
      AND (@FamilyId = 0 OR ce.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR ce.EventTitle   LIKE N'%' + @SearchWord + N'%'
           OR ce.Description  LIKE N'%' + @SearchWord + N'%'
           OR ce.Location     LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR ce.StartDateTime >= @FromDate)
      AND (@ToDate   IS NULL OR ce.StartDateTime <= @ToDate)
    ORDER BY ce.StartDateTime ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetCalendarEventById
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
        ce.Id,
        ce.EventTitle,
        ce.EventType,
        ce.Description,
        ce.StartDateTime,
        ce.EndDateTime,
        ce.IsAllDay,
        ce.Location,
        ce.ColorHex,
        ce.IsRecurring,
        ce.RecurrenceRule,
        ce.VisibilityScope,
        ce.DateCreated,
        ce.LastUpdated
    FROM dbo.tblCalendarEvent ce WITH (NOLOCK)
    WHERE ce.IsDeleted = 0
      AND (@FamilyId = 0 OR ce.FamilyId = @FamilyId)
      AND ce.Id = @ParsedId;
END
GO
