IF OBJECT_ID(N'dbo.tblLocationAlert', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLocationAlert
    (
        LocationAlertId     BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblLocationAlert_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NOT NULL,
        FamilyMemberId      BIGINT NOT NULL,
        -- AlertType: ZoneArrival / ZoneDeparture / LateAlert / SOS / BatteryWarning / LocationStale / LocationSharingPaused
        AlertType           NVARCHAR(32) NOT NULL,
        -- SafeZoneId nullable — zone may be soft-deleted; name preserved in ZoneNameSnapshot
        SafeZoneId          BIGINT NULL,
        ZoneNameSnapshot    NVARCHAR(64) NULL,
        Latitude            DECIMAL(10,7) NULL,
        Longitude           DECIMAL(10,7) NULL,
        IsResolved          BIT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_IsResolved DEFAULT (0),
        ResolvedAt          DATETIME2 NULL,
        ResolvedByUserId    BIGINT NULL,
        ResolutionNote      NVARCHAR(512) NULL,
        TriggeredAt         DATETIME2 NOT NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblLocationAlert_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblLocationAlert_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblLocationAlert_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblLocationAlert_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblLocationAlert_LocationAlertId PRIMARY KEY (LocationAlertId),
        CONSTRAINT FK_tblLocationAlert_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblLocationAlert_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT FK_tblLocationAlert_SafeZoneId_tblSafeZone_SafeZoneId
            FOREIGN KEY (SafeZoneId) REFERENCES dbo.tblSafeZone (SafeZoneId),
        CONSTRAINT FK_tblLocationAlert_ResolvedByUserId_tblUser_UserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblLocationAlert_AlertType
            CHECK (AlertType IN (
                N'ZoneArrival', N'ZoneDeparture', N'LateAlert', N'SOS',
                N'BatteryWarning', N'LocationStale', N'LocationSharingPaused'
            ))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblLocationAlert_Id' AND object_id = OBJECT_ID(N'dbo.tblLocationAlert'))
BEGIN
    CREATE UNIQUE INDEX UK_tblLocationAlert_Id ON dbo.tblLocationAlert (Id) WHERE IsDeleted = 0;
END;
GO

-- Alert history list — newest first, family scoped
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblLocationAlert_FamilyId_TriggeredAt' AND object_id = OBJECT_ID(N'dbo.tblLocationAlert'))
BEGIN
    CREATE INDEX IDX_tblLocationAlert_FamilyId_TriggeredAt
        ON dbo.tblLocationAlert (FamilyId, TriggeredAt DESC)
        WHERE IsDeleted = 0;
END;
GO

-- SOS indicator — active unresolved SOS per member on parent map
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblLocationAlert_FamilyMemberId_AlertType' AND object_id = OBJECT_ID(N'dbo.tblLocationAlert'))
BEGIN
    CREATE INDEX IDX_tblLocationAlert_FamilyMemberId_AlertType
        ON dbo.tblLocationAlert (FamilyMemberId, AlertType)
        WHERE IsDeleted = 0 AND IsResolved = 0;
END;
GO
