IF OBJECT_ID(N'dbo.Vaccinations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vaccinations
    (
        VaccinationId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Vaccinations PRIMARY KEY DEFAULT NEWID(),
        HealthProfileId  UNIQUEIDENTIFIER NOT NULL,
        FamilyId         UNIQUEIDENTIFIER NOT NULL,
        VaccineName      NVARCHAR(200)    NOT NULL,
        -- Status: Given / Due / Overdue / NotApplicable
        Status           NVARCHAR(20)     NOT NULL CONSTRAINT DF_Vaccinations_Status DEFAULT N'Due',
        GivenDate        DATE             NULL,
        DueDate          DATE             NULL,
        LinkedDocumentId UNIQUEIDENTIFIER NULL,
        CreatedAt        DATETIME2        NOT NULL CONSTRAINT DF_Vaccinations_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt        DATETIME2        NOT NULL CONSTRAINT DF_Vaccinations_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted        BIT              NOT NULL CONSTRAINT DF_Vaccinations_IsDeleted  DEFAULT 0,
        DeletedAt        DATETIME2        NULL,

        CONSTRAINT FK_Vaccinations_HealthProfiles_HealthProfileId
            FOREIGN KEY (HealthProfileId)  REFERENCES dbo.HealthProfiles (HealthProfileId),
        CONSTRAINT FK_Vaccinations_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_Vaccinations_VaultDocuments_LinkedDocumentId
            FOREIGN KEY (LinkedDocumentId) REFERENCES dbo.VaultDocuments (DocumentId),
        CONSTRAINT CK_Vaccinations_Status
            CHECK (Status IN (N'Given', N'Due', N'Overdue', N'NotApplicable'))
    );
END;
GO

-- Vaccination list per health profile
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Vaccinations_HealthProfileId'
      AND object_id = OBJECT_ID(N'dbo.Vaccinations')
)
BEGIN
    CREATE INDEX IX_Vaccinations_HealthProfileId
        ON dbo.Vaccinations (HealthProfileId)
        WHERE IsDeleted = 0;
END;
GO

-- Vaccination reminder worker — daily scan for Due vaccinations within 14 days
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Vaccinations_DueDate_Status'
      AND object_id = OBJECT_ID(N'dbo.Vaccinations')
)
BEGIN
    CREATE INDEX IX_Vaccinations_DueDate_Status
        ON dbo.Vaccinations (DueDate, Status)
        WHERE IsDeleted = 0 AND Status IN (N'Due', N'Overdue') AND DueDate IS NOT NULL;
END;
GO
