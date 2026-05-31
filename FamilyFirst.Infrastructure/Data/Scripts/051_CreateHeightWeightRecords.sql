IF OBJECT_ID(N'dbo.tblHeightWeightRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblHeightWeightRecord
    (
        HeightWeightRecordId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        HealthProfileId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        RecordedDate            DATETIME2 NOT NULL,
        HeightCm                DECIMAL(5,1) NULL,
        WeightKg                DECIMAL(5,2) NULL,
        RecordedByUserId        BIGINT NOT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblHeightWeightRecord_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblHeightWeightRecord_HeightWeightRecordId PRIMARY KEY (HeightWeightRecordId),
        CONSTRAINT FK_tblHeightWeightRecord_HealthProfileId_tblHealthProfile_HealthProfileId
            FOREIGN KEY (HealthProfileId) REFERENCES dbo.tblHealthProfile (HealthProfileId),
        CONSTRAINT FK_tblHeightWeightRecord_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblHeightWeightRecord_RecordedByUserId_tblUser_UserId
            FOREIGN KEY (RecordedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblHeightWeightRecord_HeightOrWeight
            CHECK (HeightCm IS NOT NULL OR WeightKg IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblHeightWeightRecord_Id' AND object_id = OBJECT_ID(N'dbo.tblHeightWeightRecord'))
BEGIN
    CREATE UNIQUE INDEX UK_tblHeightWeightRecord_Id ON dbo.tblHeightWeightRecord (Id) WHERE IsDeleted = 0;
END;
GO

-- Growth trend chart — sorted by date per health profile
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblHeightWeightRecord_HealthProfileId_RecordedDate' AND object_id = OBJECT_ID(N'dbo.tblHeightWeightRecord'))
BEGIN
    CREATE INDEX IDX_tblHeightWeightRecord_HealthProfileId_RecordedDate
        ON dbo.tblHeightWeightRecord (HealthProfileId, RecordedDate DESC)
        WHERE IsDeleted = 0;
END;
GO
