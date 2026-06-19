-- ============================================================
-- Script  : 098_CreateSP_RewardsSearchCode.sql
-- Module  : Rewards & Coins (REWARDS — ModuleId 7)
-- SPs:
--   uspGetRewardBySearch / uspGetRewardById
--   uspGetRewardRedemptionBySearch / uspGetRewardRedemptionById
--   uspGetCoinTransactionBySearch / uspGetCoinTransactionById
-- ============================================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── Reward ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetRewardBySearch
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
        r.Id,
        r.RewardName,
        r.IconCode,
        r.CoinCost,
        r.IsEnabled,
        r.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblReward r WITH (NOLOCK)
    WHERE r.IsDeleted  = 0
      AND r.IsPublished = 1
      AND (@FamilyId = 0 OR r.FamilyId = @FamilyId OR r.FamilyId IS NULL)
      AND (@SearchWord IS NULL OR r.RewardName LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR r.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR r.DateCreated <= @ToDate)
    ORDER BY r.SortOrder, r.RewardName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetRewardById
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
        r.Id,
        r.RewardName,
        r.IconCode,
        r.CoinCost,
        r.IsEnabled,
        r.SortOrder,
        r.DateCreated,
        r.LastUpdated
    FROM dbo.tblReward r WITH (NOLOCK)
    WHERE r.IsDeleted = 0
      AND (@FamilyId = 0 OR r.FamilyId = @FamilyId OR r.FamilyId IS NULL)
      AND r.Id = @ParsedId;
END
GO

-- ── RewardRedemption ──────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetRewardRedemptionBySearch
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
        rr.Id,
        r.Id            AS RewardGuid,
        r.RewardName,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        rr.CoinsSpent,
        rr.Status,
        rr.RequestedAt,
        rr.ReviewedAt,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblRewardRedemption rr WITH (NOLOCK)
    JOIN dbo.tblReward r WITH (NOLOCK) ON r.RewardId = rr.RewardId AND r.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = rr.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE rr.IsDeleted = 0
      AND (@FamilyId = 0 OR rr.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR r.RewardName LIKE N'%' + @SearchWord + N'%'
           OR u.FullName   LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR rr.RequestedAt >= @FromDate)
      AND (@ToDate   IS NULL OR rr.RequestedAt <= @ToDate)
    ORDER BY rr.RequestedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetRewardRedemptionById
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
        rr.Id,
        r.Id            AS RewardGuid,
        r.RewardName,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        rr.CoinsSpent,
        rr.Status,
        rr.RequestedAt,
        rr.ReviewedAt,
        rr.ParentNote
    FROM dbo.tblRewardRedemption rr WITH (NOLOCK)
    JOIN dbo.tblReward r WITH (NOLOCK) ON r.RewardId = rr.RewardId AND r.IsDeleted = 0
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = rr.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE rr.IsDeleted = 0
      AND (@FamilyId = 0 OR rr.FamilyId = @FamilyId)
      AND rr.Id = @ParsedId;
END
GO

-- ── CoinTransaction ───────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.uspGetCoinTransactionBySearch
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
        ct.Id,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        ct.TransactionType,
        ct.Amount,
        ct.BalanceAfter,
        ct.ReferenceType,
        ct.Note,
        ct.DateCreated,
        COUNT(1) OVER() AS TotalCount
    FROM dbo.tblCoinTransaction ct WITH (NOLOCK)
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ct.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE (@FamilyId = 0 OR ct.FamilyId = @FamilyId)
      AND (@SearchWord IS NULL
           OR u.FullName          LIKE N'%' + @SearchWord + N'%'
           OR ct.TransactionType  LIKE N'%' + @SearchWord + N'%'
           OR ct.ReferenceType    LIKE N'%' + @SearchWord + N'%')
      AND (@FromDate IS NULL OR ct.DateCreated >= @FromDate)
      AND (@ToDate   IS NULL OR ct.DateCreated <= @ToDate)
    ORDER BY ct.DateCreated DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.uspGetCoinTransactionById
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
        ct.Id,
        cp.Id           AS ChildProfileGuid,
        u.FullName      AS ChildName,
        ct.TransactionType,
        ct.Amount,
        ct.BalanceAfter,
        ct.ReferenceType,
        ct.ReferenceId,
        ct.Note,
        ct.DateCreated
    FROM dbo.tblCoinTransaction ct WITH (NOLOCK)
    JOIN dbo.tblChildProfile cp WITH (NOLOCK) ON cp.ChildProfileId = ct.ChildProfileId AND cp.IsDeleted = 0
    JOIN dbo.tblUser u WITH (NOLOCK) ON u.UserId = cp.UserId AND u.IsDeleted = 0
    WHERE (@FamilyId = 0 OR ct.FamilyId = @FamilyId)
      AND ct.Id = @ParsedId;
END
GO
