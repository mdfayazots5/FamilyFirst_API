-- ============================================================
-- Script  : 100_CreateSP_NotificationSearchCode.sql
-- Module  : Notification Engine (NOTIF — ModuleId 9)
-- SPs:
--   uspGetNotificationBySearch
--   uspGetNotificationById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.uspGetNotificationBySearch
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
        n.Id,
        n.Title,
        n.Body,
        n.Priority,
        n.Channel,
        n.ReferenceType,
        n.IsRead,
        n.ReadAt,
        n.IsSent,
        n.SentAt,
        n.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblNotification n WITH (NOLOCK)
    WHERE n.IsDeleted = 0
      AND (@FamilyId = 0 OR n.FamilyId = @FamilyId)
      AND (@UserId   = 0 OR n.RecipientUserId = @UserId)
      AND (@SearchWord IS NULL
           OR n.Title LIKE N'%' + @SearchWord + N'%'
           OR n.Body  LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR n.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR n.DateCreated <= @ToDate)
    ORDER BY n.DateCreated DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetNotificationById
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
        n.Id,
        n.Title,
        n.Body,
        n.Priority,
        n.Channel,
        n.ReferenceType,
        n.ReferenceId,
        n.DeepLinkPath,
        n.IsRead,
        n.ReadAt,
        n.IsSent,
        n.SentAt,
        n.FcmMessageId,
        n.DateCreated
    FROM dbo.tblNotification n WITH (NOLOCK)
    WHERE n.IsDeleted = 0
      AND (@FamilyId = 0 OR n.FamilyId = @FamilyId)
      AND n.Id = @ParsedId;
END
GO
