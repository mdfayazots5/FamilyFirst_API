IF OBJECT_ID(N'dbo.tblSOSEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSOSEvent
    (
        SOSEventId          BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblSOSEvent_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NOT NULL,
        ChildProfileId      BIGINT NOT NULL,
        LocationAlertId     BIGINT NOT NULL,
        Latitude            DECIMAL(10,7) NOT NULL,
        Longitude           DECIMAL(10,7) NOT NULL,
        DispatchedAt        DATETIME2 NOT NULL,
        AlertsSentCount     INT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_AlertsSentCount DEFAULT (0),
        ResolvedAt          DATETIME2 NULL,
        ResolvedByUserId    BIGINT NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblSOSEvent_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblSOSEvent_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblSOSEvent_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblSOSEvent_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblSOSEvent_SOSEventId PRIMARY KEY (SOSEventId),
        CONSTRAINT FK_tblSOSEvent_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblSOSEvent_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblSOSEvent_LocationAlertId_tblLocationAlert_LocationAlertId
            FOREIGN KEY (LocationAlertId) REFERENCES dbo.tblLocationAlert (LocationAlertId),
        CONSTRAINT FK_tblSOSEvent_ResolvedByUserId_tblUser_UserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblSOSEvent_Id' AND object_id = OBJECT_ID(N'dbo.tblSOSEvent'))
BEGIN
    CREATE UNIQUE INDEX UK_tblSOSEvent_Id ON dbo.tblSOSEvent (Id) WHERE IsDeleted = 0;
END;
GO

-- Active (unresolved) SOS events per family — parent map SOS indicator
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblSOSEvent_FamilyId_ResolvedAt' AND object_id = OBJECT_ID(N'dbo.tblSOSEvent'))
BEGIN
    CREATE INDEX IDX_tblSOSEvent_FamilyId_ResolvedAt
        ON dbo.tblSOSEvent (FamilyId, ResolvedAt)
        WHERE IsDeleted = 0 AND ResolvedAt IS NULL;
END;
GO
