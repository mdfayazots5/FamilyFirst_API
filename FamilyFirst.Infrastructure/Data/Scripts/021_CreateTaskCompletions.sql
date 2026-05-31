IF OBJECT_ID(N'dbo.tblTaskCompletion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTaskCompletion
    (
        TaskCompletionId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        TaskItemId          BIGINT NOT NULL,
        ChildProfileId      BIGINT NOT NULL,
        FamilyId            BIGINT NOT NULL,
        ScheduledDate       DATETIME2 NOT NULL,
        Status              INT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_Status DEFAULT (1),
        PhotoUrl            NVARCHAR(512) NULL,
        SubmittedAt         DATETIME2 NULL,
        ReviewedByUserId    BIGINT NULL,
        ReviewedAt          DATETIME2 NULL,
        ReviewNote          NVARCHAR(512) NULL,
        CoinsAwarded        INT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_CoinsAwarded DEFAULT (0),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblTaskCompletion_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTaskCompletion_TaskCompletionId PRIMARY KEY (TaskCompletionId),
        CONSTRAINT FK_tblTaskCompletion_TaskItemId_tblTaskItem_TaskItemId
            FOREIGN KEY (TaskItemId) REFERENCES dbo.tblTaskItem (TaskItemId),
        CONSTRAINT FK_tblTaskCompletion_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblTaskCompletion_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblTaskCompletion_ReviewedByUserId_tblUser_UserId
            FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTaskCompletion_Id' AND object_id = OBJECT_ID(N'dbo.tblTaskCompletion'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTaskCompletion_Id ON dbo.tblTaskCompletion (Id) WHERE IsDeleted = 0;
END;
GO

-- One completion record per task per child per scheduled date
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTaskCompletion_TaskItemId_ChildProfileId_ScheduledDate' AND object_id = OBJECT_ID(N'dbo.tblTaskCompletion'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTaskCompletion_TaskItemId_ChildProfileId_ScheduledDate
        ON dbo.tblTaskCompletion (TaskItemId, ChildProfileId, ScheduledDate)
        WHERE IsDeleted = 0;
END;
GO
