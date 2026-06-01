-- ============================================================
-- Script  : 092_CreateLookupTables.sql
-- Purpose : Create all FamilyFirst simple lookup tables.
--           Every MasterDataCode routes to its own dedicated table.
--           No inline values are stored in tblMasterData — all data
--           comes from these tables. Mirrors the reference DB pattern
--           where each master data code has its own table
--           (tblRole, tblOTPType, tblGender, tblModule, etc.)
-- Depends : None
-- ============================================================

-- ── tblTaskType ───────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblTaskType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTaskType
    (
        TaskTypeId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblTaskType_Id DEFAULT (NEWID()),
        TaskTypeName    NVARCHAR(128) NOT NULL,
        TaskTypeCode    NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblTaskType_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblTaskType_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblTaskType_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblTaskType_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblTaskType_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblTaskType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblTaskType_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblTaskType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblTaskType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblTaskType_TaskTypeId PRIMARY KEY (TaskTypeId)
    );
    CREATE UNIQUE INDEX UK_tblTaskType_TaskTypeCode ON dbo.tblTaskType (TaskTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblTaskStatus ─────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblTaskStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTaskStatus
    (
        TaskStatusId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblTaskStatus_Id DEFAULT (NEWID()),
        TaskStatusName      NVARCHAR(128) NOT NULL,
        TaskStatusCode      NVARCHAR(64) NOT NULL,
        CompanyId           INT NOT NULL CONSTRAINT DF_tblTaskStatus_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL CONSTRAINT DF_tblTaskStatus_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL CONSTRAINT DF_tblTaskStatus_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL CONSTRAINT DF_tblTaskStatus_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL CONSTRAINT DF_tblTaskStatus_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL CONSTRAINT DF_tblTaskStatus_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL CONSTRAINT DF_tblTaskStatus_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL CONSTRAINT DF_tblTaskStatus_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL CONSTRAINT DF_tblTaskStatus_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblTaskStatus_TaskStatusId PRIMARY KEY (TaskStatusId)
    );
    CREATE UNIQUE INDEX UK_tblTaskStatus_TaskStatusCode ON dbo.tblTaskStatus (TaskStatusCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblAttendanceStatus ───────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblAttendanceStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAttendanceStatus
    (
        AttendanceStatusId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblAttendanceStatus_Id DEFAULT (NEWID()),
        AttendanceStatusName    NVARCHAR(128) NOT NULL,
        AttendanceStatusCode    NVARCHAR(64) NOT NULL,
        CompanyId               INT NOT NULL CONSTRAINT DF_tblAttendanceStatus_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL CONSTRAINT DF_tblAttendanceStatus_SiteId DEFAULT (1),
        DepartmentId            INT NULL,
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL CONSTRAINT DF_tblAttendanceStatus_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL CONSTRAINT DF_tblAttendanceStatus_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL CONSTRAINT DF_tblAttendanceStatus_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL CONSTRAINT DF_tblAttendanceStatus_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL CONSTRAINT DF_tblAttendanceStatus_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL CONSTRAINT DF_tblAttendanceStatus_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL CONSTRAINT DF_tblAttendanceStatus_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblAttendanceStatus_AttendanceStatusId PRIMARY KEY (AttendanceStatusId)
    );
    CREATE UNIQUE INDEX UK_tblAttendanceStatus_Code ON dbo.tblAttendanceStatus (AttendanceStatusCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblRewardType ─────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblRewardType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRewardType
    (
        RewardTypeId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblRewardType_Id DEFAULT (NEWID()),
        RewardTypeName      NVARCHAR(128) NOT NULL,
        RewardTypeCode      NVARCHAR(64) NOT NULL,
        CompanyId           INT NOT NULL CONSTRAINT DF_tblRewardType_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL CONSTRAINT DF_tblRewardType_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL CONSTRAINT DF_tblRewardType_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL CONSTRAINT DF_tblRewardType_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL CONSTRAINT DF_tblRewardType_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL CONSTRAINT DF_tblRewardType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL CONSTRAINT DF_tblRewardType_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL CONSTRAINT DF_tblRewardType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL CONSTRAINT DF_tblRewardType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblRewardType_RewardTypeId PRIMARY KEY (RewardTypeId)
    );
    CREATE UNIQUE INDEX UK_tblRewardType_RewardTypeCode ON dbo.tblRewardType (RewardTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblCoinTransactionType ────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblCoinTransactionType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCoinTransactionType
    (
        CoinTransactionTypeId       BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblCoinTransactionType_Id DEFAULT (NEWID()),
        CoinTransactionTypeName     NVARCHAR(128) NOT NULL,
        CoinTransactionTypeCode     NVARCHAR(64) NOT NULL,
        CompanyId                   INT NOT NULL CONSTRAINT DF_tblCoinTransactionType_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL CONSTRAINT DF_tblCoinTransactionType_SiteId DEFAULT (1),
        DepartmentId                INT NULL,
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL CONSTRAINT DF_tblCoinTransactionType_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL CONSTRAINT DF_tblCoinTransactionType_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL CONSTRAINT DF_tblCoinTransactionType_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL CONSTRAINT DF_tblCoinTransactionType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL CONSTRAINT DF_tblCoinTransactionType_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL CONSTRAINT DF_tblCoinTransactionType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL CONSTRAINT DF_tblCoinTransactionType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblCoinTransactionType_Id PRIMARY KEY (CoinTransactionTypeId)
    );
    CREATE UNIQUE INDEX UK_tblCoinTransactionType_Code ON dbo.tblCoinTransactionType (CoinTransactionTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblFeedbackRating ─────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblFeedbackRating', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFeedbackRating
    (
        FeedbackRatingId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblFeedbackRating_Id DEFAULT (NEWID()),
        FeedbackRatingName      NVARCHAR(128) NOT NULL,
        FeedbackRatingCode      NVARCHAR(64) NOT NULL,
        CompanyId               INT NOT NULL CONSTRAINT DF_tblFeedbackRating_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL CONSTRAINT DF_tblFeedbackRating_SiteId DEFAULT (1),
        DepartmentId            INT NULL,
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL CONSTRAINT DF_tblFeedbackRating_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL CONSTRAINT DF_tblFeedbackRating_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL CONSTRAINT DF_tblFeedbackRating_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL CONSTRAINT DF_tblFeedbackRating_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL CONSTRAINT DF_tblFeedbackRating_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL CONSTRAINT DF_tblFeedbackRating_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL CONSTRAINT DF_tblFeedbackRating_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblFeedbackRating_FeedbackRatingId PRIMARY KEY (FeedbackRatingId)
    );
    CREATE UNIQUE INDEX UK_tblFeedbackRating_Code ON dbo.tblFeedbackRating (FeedbackRatingCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblCalendarEventType ──────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblCalendarEventType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCalendarEventType
    (
        CalendarEventTypeId         BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblCalendarEventType_Id DEFAULT (NEWID()),
        CalendarEventTypeName       NVARCHAR(128) NOT NULL,
        CalendarEventTypeCode       NVARCHAR(64) NOT NULL,
        CompanyId                   INT NOT NULL CONSTRAINT DF_tblCalendarEventType_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL CONSTRAINT DF_tblCalendarEventType_SiteId DEFAULT (1),
        DepartmentId                INT NULL,
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL CONSTRAINT DF_tblCalendarEventType_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL CONSTRAINT DF_tblCalendarEventType_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL CONSTRAINT DF_tblCalendarEventType_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL CONSTRAINT DF_tblCalendarEventType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL CONSTRAINT DF_tblCalendarEventType_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL CONSTRAINT DF_tblCalendarEventType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL CONSTRAINT DF_tblCalendarEventType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblCalendarEventType_Id PRIMARY KEY (CalendarEventTypeId)
    );
    CREATE UNIQUE INDEX UK_tblCalendarEventType_Code ON dbo.tblCalendarEventType (CalendarEventTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblNotificationType ───────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblNotificationType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblNotificationType
    (
        NotificationTypeId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblNotificationType_Id DEFAULT (NEWID()),
        NotificationTypeName    NVARCHAR(128) NOT NULL,
        NotificationTypeCode    NVARCHAR(64) NOT NULL,
        CompanyId               INT NOT NULL CONSTRAINT DF_tblNotificationType_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL CONSTRAINT DF_tblNotificationType_SiteId DEFAULT (1),
        DepartmentId            INT NULL,
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL CONSTRAINT DF_tblNotificationType_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL CONSTRAINT DF_tblNotificationType_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL CONSTRAINT DF_tblNotificationType_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL CONSTRAINT DF_tblNotificationType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL CONSTRAINT DF_tblNotificationType_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL CONSTRAINT DF_tblNotificationType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL CONSTRAINT DF_tblNotificationType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblNotificationType_NotificationTypeId PRIMARY KEY (NotificationTypeId)
    );
    CREATE UNIQUE INDEX UK_tblNotificationType_Code ON dbo.tblNotificationType (NotificationTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblOTPType ────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblOTPType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblOTPType
    (
        OTPTypeId       BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblOTPType_Id DEFAULT (NEWID()),
        OTPTypeName     NVARCHAR(128) NOT NULL,
        OTPTypeCode     NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblOTPType_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblOTPType_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblOTPType_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblOTPType_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblOTPType_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblOTPType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblOTPType_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblOTPType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblOTPType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblOTPType_OTPTypeId PRIMARY KEY (OTPTypeId)
    );
    CREATE UNIQUE INDEX UK_tblOTPType_OTPTypeCode ON dbo.tblOTPType (OTPTypeCode) WHERE IsDeleted = 0;
END;
GO
