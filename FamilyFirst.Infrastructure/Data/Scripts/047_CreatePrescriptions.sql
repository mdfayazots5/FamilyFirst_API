IF OBJECT_ID(N'dbo.Prescriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Prescriptions
    (
        PrescriptionId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Prescriptions PRIMARY KEY DEFAULT NEWID(),
        HealthProfileId   UNIQUEIDENTIFIER NOT NULL,
        FamilyId          UNIQUEIDENTIFIER NOT NULL,
        MedicationName    NVARCHAR(300)    NOT NULL,
        Dosage            NVARCHAR(100)    NOT NULL,
        Frequency         NVARCHAR(100)    NOT NULL,
        PrescribingDoctor NVARCHAR(200)    NOT NULL,
        StartDate         DATE             NOT NULL,
        EndDate           DATE             NULL,
        IsRecurring       BIT              NOT NULL CONSTRAINT DF_Prescriptions_IsRecurring  DEFAULT 0,
        IsArchived        BIT              NOT NULL CONSTRAINT DF_Prescriptions_IsArchived   DEFAULT 0,
        ArchivedAt        DATETIME2        NULL,
        LinkedDocumentId  UNIQUEIDENTIFIER NULL,
        CreatedAt         DATETIME2        NOT NULL CONSTRAINT DF_Prescriptions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt         DATETIME2        NOT NULL CONSTRAINT DF_Prescriptions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted         BIT              NOT NULL CONSTRAINT DF_Prescriptions_IsDeleted  DEFAULT 0,
        DeletedAt         DATETIME2        NULL,

        CONSTRAINT FK_Prescriptions_HealthProfiles_HealthProfileId
            FOREIGN KEY (HealthProfileId)  REFERENCES dbo.HealthProfiles  (HealthProfileId),
        CONSTRAINT FK_Prescriptions_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families        (FamilyId),
        CONSTRAINT FK_Prescriptions_VaultDocuments_LinkedDocumentId
            FOREIGN KEY (LinkedDocumentId) REFERENCES dbo.VaultDocuments  (DocumentId),
        CONSTRAINT CK_Prescriptions_EndDate
            CHECK (EndDate IS NULL OR EndDate >= StartDate)
    );
END;
GO

-- Active prescriptions per health profile — main profile view query
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Prescriptions_HealthProfileId_IsArchived'
      AND object_id = OBJECT_ID(N'dbo.Prescriptions')
)
BEGIN
    CREATE INDEX IX_Prescriptions_HealthProfileId_IsArchived
        ON dbo.Prescriptions (HealthProfileId, IsArchived)
        WHERE IsDeleted = 0;
END;
GO

-- Auto-archive worker scan — find prescriptions past their EndDate
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Prescriptions_EndDate_IsArchived'
      AND object_id = OBJECT_ID(N'dbo.Prescriptions')
)
BEGIN
    CREATE INDEX IX_Prescriptions_EndDate_IsArchived
        ON dbo.Prescriptions (EndDate, IsArchived)
        WHERE IsDeleted = 0 AND IsArchived = 0 AND EndDate IS NOT NULL;
END;
GO
