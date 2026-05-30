IF OBJECT_ID(N'dbo.HealthRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HealthRecords
    (
        HealthRecordId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HealthRecords PRIMARY KEY DEFAULT NEWID(),
        HealthProfileId  UNIQUEIDENTIFIER NOT NULL,
        FamilyId         UNIQUEIDENTIFIER NOT NULL,
        -- EventType: Prescription / Vaccination / HospitalVisit / TestReport / DoctorNote / AllergyUpdate
        EventType        NVARCHAR(30)     NOT NULL,
        EventDate        DATE             NOT NULL,
        Title            NVARCHAR(300)    NOT NULL,
        Notes            NVARCHAR(2000)   NULL,
        LinkedDocumentId UNIQUEIDENTIFIER NULL,
        CreatedAt        DATETIME2        NOT NULL CONSTRAINT DF_HealthRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt        DATETIME2        NOT NULL CONSTRAINT DF_HealthRecords_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted        BIT              NOT NULL CONSTRAINT DF_HealthRecords_IsDeleted  DEFAULT 0,
        DeletedAt        DATETIME2        NULL,

        CONSTRAINT FK_HealthRecords_HealthProfiles_HealthProfileId
            FOREIGN KEY (HealthProfileId)  REFERENCES dbo.HealthProfiles (HealthProfileId),
        CONSTRAINT FK_HealthRecords_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_HealthRecords_VaultDocuments_LinkedDocumentId
            FOREIGN KEY (LinkedDocumentId) REFERENCES dbo.VaultDocuments (DocumentId),
        CONSTRAINT CK_HealthRecords_EventType
            CHECK (EventType IN (
                N'Prescription', N'Vaccination', N'HospitalVisit',
                N'TestReport', N'DoctorNote', N'AllergyUpdate'
            ))
    );
END;
GO

-- Health Timeline query — newest first per health profile with optional event type filter
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HealthRecords_HealthProfileId_EventDate'
      AND object_id = OBJECT_ID(N'dbo.HealthRecords')
)
BEGIN
    CREATE INDEX IX_HealthRecords_HealthProfileId_EventDate
        ON dbo.HealthRecords (HealthProfileId, EventDate DESC)
        WHERE IsDeleted = 0;
END;
GO
