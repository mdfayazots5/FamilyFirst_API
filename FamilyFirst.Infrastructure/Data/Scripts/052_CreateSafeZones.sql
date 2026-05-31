IF OBJECT_ID(N'dbo.tblSafeZone', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSafeZone
    (
        SafeZoneId              BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblSafeZone_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        ZoneName                NVARCHAR(64) NOT NULL,
        -- ZoneType: Home / School / Tuition / RelativesHouse / Workplace / PlaceOfWorship / Other
        ZoneType                NVARCHAR(32) NOT NULL,
        CenterLatitude          DECIMAL(10,7) NOT NULL,
        CenterLongitude         DECIMAL(10,7) NOT NULL,
        RadiusMetres            INT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_RadiusMetres DEFAULT (150),
        AlertOnArrival          BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_AlertOnArrival DEFAULT (1),
        AlertOnDeparture        BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_AlertOnDeparture DEFAULT (1),
        LateAlertEnabled        BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_LateAlertEnabled DEFAULT (0),
        LateAlertTime           DATETIME2 NULL,
        OverrideQuietHours      BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_OverrideQuietHours DEFAULT (1),
        AppliedMemberIdsJson    NVARCHAR(2048) NOT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblSafeZone_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblSafeZone_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblSafeZone_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblSafeZone_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblSafeZone_SafeZoneId PRIMARY KEY (SafeZoneId),
        CONSTRAINT FK_tblSafeZone_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        -- Business rule: ZoneName max 40 chars (storage is 64 for standard alignment)
        CONSTRAINT CK_tblSafeZone_ZoneName
            CHECK (LEN(ZoneName) BETWEEN 1 AND 40),
        CONSTRAINT CK_tblSafeZone_RadiusMetres
            CHECK (RadiusMetres BETWEEN 50 AND 500),
        CONSTRAINT CK_tblSafeZone_ZoneType
            CHECK (ZoneType IN (
                N'Home', N'School', N'Tuition', N'RelativesHouse',
                N'Workplace', N'PlaceOfWorship', N'Other'
            )),
        CONSTRAINT CK_tblSafeZone_LateAlertTime
            CHECK (LateAlertEnabled = 0 OR LateAlertTime IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblSafeZone_Id' AND object_id = OBJECT_ID(N'dbo.tblSafeZone'))
BEGIN
    CREATE UNIQUE INDEX UK_tblSafeZone_Id ON dbo.tblSafeZone (Id) WHERE IsDeleted = 0;
END;
GO

-- Row-level security — family map query
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblSafeZone_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblSafeZone'))
BEGIN
    CREATE INDEX IDX_tblSafeZone_FamilyId
        ON dbo.tblSafeZone (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
