IF OBJECT_ID(N'dbo.tblVaccination', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaccination
    (
        VaccinationId           BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblVaccination_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblVaccination_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblVaccination_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        HealthProfileId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        VaccineName             NVARCHAR(256) NOT NULL,
        -- Status: Given / Due / Overdue / NotApplicable
        Status                  NVARCHAR(24) NOT NULL
                                    CONSTRAINT DF_tblVaccination_Status DEFAULT (N'Due'),
        GivenDate               DATETIME2 NULL,
        DueDate                 DATETIME2 NULL,
        LinkedVaultDocumentId   BIGINT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblVaccination_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblVaccination_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblVaccination_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblVaccination_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblVaccination_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblVaccination_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblVaccination_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaccination_VaccinationId PRIMARY KEY (VaccinationId),
        CONSTRAINT FK_tblVaccination_HealthProfileId_tblHealthProfile_HealthProfileId
            FOREIGN KEY (HealthProfileId) REFERENCES dbo.tblHealthProfile (HealthProfileId),
        CONSTRAINT FK_tblVaccination_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblVaccination_LinkedVaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (LinkedVaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT CK_tblVaccination_Status
            CHECK (Status IN (N'Given', N'Due', N'Overdue', N'NotApplicable'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaccination_Id' AND object_id = OBJECT_ID(N'dbo.tblVaccination'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaccination_Id ON dbo.tblVaccination (Id) WHERE IsDeleted = 0;
END;
GO

-- Vaccination list per health profile
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaccination_HealthProfileId' AND object_id = OBJECT_ID(N'dbo.tblVaccination'))
BEGIN
    CREATE INDEX IDX_tblVaccination_HealthProfileId
        ON dbo.tblVaccination (HealthProfileId)
        WHERE IsDeleted = 0;
END;
GO

-- Vaccination reminder worker — daily scan for Due vaccinations within 14 days
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaccination_DueDate_Status' AND object_id = OBJECT_ID(N'dbo.tblVaccination'))
BEGIN
    CREATE INDEX IDX_tblVaccination_DueDate_Status
        ON dbo.tblVaccination (DueDate, Status)
        WHERE IsDeleted = 0 AND Status IN (N'Due', N'Overdue') AND DueDate IS NOT NULL;
END;
GO
