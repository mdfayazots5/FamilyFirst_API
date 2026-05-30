-- LocationHistory is append-only — no IsDeleted / UpdatedAt / BaseEntity columns.
-- Rows are hard-deleted by SafetyWorker after 30 days (DPDP Act 2023 requirement).
IF OBJECT_ID(N'dbo.LocationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocationHistory
    (
        LocationHistoryId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LocationHistory PRIMARY KEY DEFAULT NEWID(),
        FamilyId            UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId      UNIQUEIDENTIFIER NOT NULL,
        Latitude            DECIMAL(10,7)    NOT NULL,
        Longitude           DECIMAL(10,7)    NOT NULL,
        BatteryLevel        INT              NOT NULL,
        LocationName        NVARCHAR(300)    NULL,
        RecordedAt          DATETIME2        NOT NULL,
        CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_LocationHistory_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_LocationHistory_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_LocationHistory_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT CK_LocationHistory_BatteryLevel
            CHECK (BatteryLevel BETWEEN 0 AND 100)
    );
END;
GO

-- Last-known location per member (map view + stale detection) + SafetyWorker 30-day purge
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LocationHistory_FamilyMemberId_RecordedAt'
      AND object_id = OBJECT_ID(N'dbo.LocationHistory')
)
BEGIN
    CREATE INDEX IX_LocationHistory_FamilyMemberId_RecordedAt
        ON dbo.LocationHistory (FamilyMemberId, RecordedAt DESC);
END;
GO
