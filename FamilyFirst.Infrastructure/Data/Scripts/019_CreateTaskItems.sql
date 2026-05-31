IF OBJECT_ID(N'dbo.tblTaskItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTaskItem
    (
        TaskItemId          BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblTaskItem_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblTaskItem_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblTaskItem_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NULL,
        ChildProfileId      BIGINT NULL,
        CreatedByUserId     BIGINT NOT NULL,
        TaskName            NVARCHAR(256) NOT NULL,
        Instructions        NVARCHAR(512) NULL,
        IconCode            NVARCHAR(64) NULL,
        TimeBlock           INT NOT NULL,
        DurationMinutes     INT NOT NULL
                                CONSTRAINT DF_tblTaskItem_DurationMinutes DEFAULT (15),
        CoinValue           INT NOT NULL
                                CONSTRAINT DF_tblTaskItem_CoinValue DEFAULT (10),
        IsPhotoRequired     BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsPhotoRequired DEFAULT (0),
        PillarTag           NVARCHAR(64) NULL,
        IsRecurring         BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsRecurring DEFAULT (1),
        RecurringDays       NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblTaskItem_RecurringDays DEFAULT (N'[1,2,3,4,5,6,7]'),
        ActiveFromDate      DATETIME2 NOT NULL
                                CONSTRAINT DF_tblTaskItem_ActiveFromDate DEFAULT (GETDATE()),
        ActiveToDate        DATETIME2 NULL,
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsActive DEFAULT (1),
        IsSystemTemplate    BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsSystemTemplate DEFAULT (0),
        TemplateCategory    NVARCHAR(64) NULL,
        AgeGroup            NVARCHAR(64) NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblTaskItem_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblTaskItem_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblTaskItem_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblTaskItem_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblTaskItem_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTaskItem_TaskItemId PRIMARY KEY (TaskItemId),
        CONSTRAINT FK_tblTaskItem_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblTaskItem_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblTaskItem_CreatedByUserId_tblUser_UserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblTaskItem_RecurringDaysJson
            CHECK (ISJSON(RecurringDays) = 1),
        CONSTRAINT CK_tblTaskItem_ActiveDateRange
            CHECK (ActiveToDate IS NULL OR ActiveToDate > ActiveFromDate),
        CONSTRAINT CK_tblTaskItem_PillarTag
            CHECK (PillarTag IS NULL OR PillarTag IN (N'Study', N'Cleanliness', N'Discipline', N'ScreenControl', N'Responsibility')),
        CONSTRAINT CK_tblTaskItem_TemplateShape
            CHECK
            (
                (IsSystemTemplate = 0 AND FamilyId IS NOT NULL)
                OR
                (IsSystemTemplate = 1 AND FamilyId IS NULL AND ChildProfileId IS NULL AND TemplateCategory IS NOT NULL)
            )
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTaskItem_Id' AND object_id = OBJECT_ID(N'dbo.tblTaskItem'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTaskItem_Id ON dbo.tblTaskItem (Id) WHERE IsDeleted = 0;
END;
GO
