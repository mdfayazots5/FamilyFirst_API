IF OBJECT_ID(N'dbo.tblPrescription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblPrescription
    (
        PrescriptionId          BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblPrescription_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblPrescription_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblPrescription_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        HealthProfileId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        MedicationName          NVARCHAR(512) NOT NULL,
        Dosage                  NVARCHAR(128) NOT NULL,
        Frequency               NVARCHAR(128) NOT NULL,
        PrescribingDoctor       NVARCHAR(256) NOT NULL,
        StartDate               DATETIME2 NOT NULL,
        EndDate                 DATETIME2 NULL,
        IsRecurring             BIT NOT NULL
                                    CONSTRAINT DF_tblPrescription_IsRecurring DEFAULT (0),
        IsArchived              BIT NOT NULL
                                    CONSTRAINT DF_tblPrescription_IsArchived DEFAULT (0),
        ArchivedAt              DATETIME2 NULL,
        LinkedVaultDocumentId   BIGINT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblPrescription_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblPrescription_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblPrescription_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblPrescription_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblPrescription_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblPrescription_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblPrescription_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblPrescription_PrescriptionId PRIMARY KEY (PrescriptionId),
        CONSTRAINT FK_tblPrescription_HealthProfileId_tblHealthProfile_HealthProfileId
            FOREIGN KEY (HealthProfileId) REFERENCES dbo.tblHealthProfile (HealthProfileId),
        CONSTRAINT FK_tblPrescription_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblPrescription_LinkedVaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (LinkedVaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT CK_tblPrescription_EndDate
            CHECK (EndDate IS NULL OR EndDate >= StartDate)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblPrescription_Id' AND object_id = OBJECT_ID(N'dbo.tblPrescription'))
BEGIN
    CREATE UNIQUE INDEX UK_tblPrescription_Id ON dbo.tblPrescription (Id) WHERE IsDeleted = 0;
END;
GO

-- Active prescriptions per health profile — main profile view query
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblPrescription_HealthProfileId_IsArchived' AND object_id = OBJECT_ID(N'dbo.tblPrescription'))
BEGIN
    CREATE INDEX IDX_tblPrescription_HealthProfileId_IsArchived
        ON dbo.tblPrescription (HealthProfileId, IsArchived)
        WHERE IsDeleted = 0;
END;
GO

-- Auto-archive worker scan — find prescriptions past their EndDate
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblPrescription_EndDate_IsArchived' AND object_id = OBJECT_ID(N'dbo.tblPrescription'))
BEGIN
    CREATE INDEX IDX_tblPrescription_EndDate_IsArchived
        ON dbo.tblPrescription (EndDate, IsArchived)
        WHERE IsDeleted = 0 AND IsArchived = 0 AND EndDate IS NOT NULL;
END;
GO
