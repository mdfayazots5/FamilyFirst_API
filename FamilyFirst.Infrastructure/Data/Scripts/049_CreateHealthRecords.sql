IF OBJECT_ID(N'dbo.tblHealthRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblHealthRecord
    (
        HealthRecordId          BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        HealthProfileId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        -- EventType: Prescription / Vaccination / HospitalVisit / TestReport / DoctorNote / AllergyUpdate
        EventType               NVARCHAR(32) NOT NULL,
        EventDate               DATETIME2 NOT NULL,
        Title                   NVARCHAR(512) NOT NULL,
        Notes                   NVARCHAR(2048) NULL,
        LinkedVaultDocumentId   BIGINT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblHealthRecord_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblHealthRecord_HealthRecordId PRIMARY KEY (HealthRecordId),
        CONSTRAINT FK_tblHealthRecord_HealthProfileId_tblHealthProfile_HealthProfileId
            FOREIGN KEY (HealthProfileId) REFERENCES dbo.tblHealthProfile (HealthProfileId),
        CONSTRAINT FK_tblHealthRecord_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblHealthRecord_LinkedVaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (LinkedVaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT CK_tblHealthRecord_EventType
            CHECK (EventType IN (
                N'Prescription', N'Vaccination', N'HospitalVisit',
                N'TestReport', N'DoctorNote', N'AllergyUpdate'
            ))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblHealthRecord_Id' AND object_id = OBJECT_ID(N'dbo.tblHealthRecord'))
BEGIN
    CREATE UNIQUE INDEX UK_tblHealthRecord_Id ON dbo.tblHealthRecord (Id) WHERE IsDeleted = 0;
END;
GO

-- Health Timeline query — newest first per health profile with optional event type filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblHealthRecord_HealthProfileId_EventDate' AND object_id = OBJECT_ID(N'dbo.tblHealthRecord'))
BEGIN
    CREATE INDEX IDX_tblHealthRecord_HealthProfileId_EventDate
        ON dbo.tblHealthRecord (HealthProfileId, EventDate DESC)
        WHERE IsDeleted = 0;
END;
GO
