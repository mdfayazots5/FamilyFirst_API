-- ReferenceId is a soft (non-FK) reference to the triggering entity's BIGINT PK;
-- the referenced table varies based on ReferenceType (TaskCompletion, CalendarEvent, etc.).
IF OBJECT_ID(N'dbo.tblNotification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblNotification
    (
        NotificationId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblNotification_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblNotification_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblNotification_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NULL,
        RecipientUserId     BIGINT NOT NULL,
        Title               NVARCHAR(256) NOT NULL,
        Body                NVARCHAR(1024) NOT NULL,
        Priority            INT NOT NULL
                                CONSTRAINT DF_tblNotification_Priority DEFAULT (2),
        Channel             INT NOT NULL
                                CONSTRAINT DF_tblNotification_Channel DEFAULT (1),
        ReferenceType       NVARCHAR(64) NULL,
        ReferenceId         BIGINT NULL,
        DeepLinkPath        NVARCHAR(512) NULL,
        IsRead              BIT NOT NULL
                                CONSTRAINT DF_tblNotification_IsRead DEFAULT (0),
        ReadAt              DATETIME2 NULL,
        IsSent              BIT NOT NULL
                                CONSTRAINT DF_tblNotification_IsSent DEFAULT (0),
        SentAt              DATETIME2 NULL,
        FcmMessageId        NVARCHAR(256) NULL,
        IsBatched           BIT NOT NULL
                                CONSTRAINT DF_tblNotification_IsBatched DEFAULT (0),
        BatchGroup          NVARCHAR(64) NULL,
        ScheduledFor        DATETIME2 NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblNotification_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblNotification_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblNotification_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblNotification_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblNotification_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblNotification_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblNotification_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblNotification_NotificationId PRIMARY KEY (NotificationId),
        CONSTRAINT FK_tblNotification_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblNotification_RecipientUserId_tblUser_UserId
            FOREIGN KEY (RecipientUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblNotification_Id' AND object_id = OBJECT_ID(N'dbo.tblNotification'))
BEGIN
    CREATE UNIQUE INDEX UK_tblNotification_Id ON dbo.tblNotification (Id) WHERE IsDeleted = 0;
END;
GO
