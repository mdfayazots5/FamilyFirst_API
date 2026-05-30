IF OBJECT_ID(N'dbo.HeightWeightRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HeightWeightRecords
    (
        HeightWeightRecordId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HeightWeightRecords PRIMARY KEY DEFAULT NEWID(),
        HealthProfileId      UNIQUEIDENTIFIER NOT NULL,
        FamilyId             UNIQUEIDENTIFIER NOT NULL,
        RecordedDate         DATE             NOT NULL,
        HeightCm             DECIMAL(5,1)     NULL,
        WeightKg             DECIMAL(5,2)     NULL,
        RecordedByUserId     UNIQUEIDENTIFIER NOT NULL,
        CreatedAt            DATETIME2        NOT NULL CONSTRAINT DF_HeightWeightRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt            DATETIME2        NOT NULL CONSTRAINT DF_HeightWeightRecords_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_HeightWeightRecords_IsDeleted  DEFAULT 0,
        DeletedAt            DATETIME2        NULL,

        CONSTRAINT FK_HeightWeightRecords_HealthProfiles_HealthProfileId
            FOREIGN KEY (HealthProfileId)   REFERENCES dbo.HealthProfiles (HealthProfileId),
        CONSTRAINT FK_HeightWeightRecords_Families_FamilyId
            FOREIGN KEY (FamilyId)          REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_HeightWeightRecords_Users_RecordedByUserId
            FOREIGN KEY (RecordedByUserId)  REFERENCES dbo.Users          (UserId),
        CONSTRAINT CK_HeightWeightRecords_HeightOrWeight
            CHECK (HeightCm IS NOT NULL OR WeightKg IS NOT NULL)
    );
END;
GO

-- Growth trend chart — sorted by date per health profile
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HeightWeightRecords_HealthProfileId_RecordedDate'
      AND object_id = OBJECT_ID(N'dbo.HeightWeightRecords')
)
BEGIN
    CREATE INDEX IX_HeightWeightRecords_HealthProfileId_RecordedDate
        ON dbo.HeightWeightRecords (HealthProfileId, RecordedDate DESC)
        WHERE IsDeleted = 0;
END;
GO
