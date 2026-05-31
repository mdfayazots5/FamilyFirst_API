-- tblLocationHistory is append-only. Rows are hard-deleted by SafetyWorker after 30 days
-- (DPDP Act 2023 privacy requirement). UpdatedBy/LastUpdated/DeletedBy/DateDeleted/IsDeleted
-- are omitted — justified: records are never modified or soft-deleted.
IF OBJECT_ID(N'dbo.tblLocationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLocationHistory
    (
        LocationHistoryId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblLocationHistory_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblLocationHistory_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblLocationHistory_SiteId DEFAULT (1),

        -- Business columns
        FamilyId            BIGINT NOT NULL,
        FamilyMemberId      BIGINT NOT NULL,
        Latitude            DECIMAL(10,7) NOT NULL,
        Longitude           DECIMAL(10,7) NOT NULL,
        BatteryLevel        INT NOT NULL,
        LocationName        NVARCHAR(512) NULL,
        RecordedAt          DATETIME2 NOT NULL,

        -- Minimal audit columns (append-only — no update or delete columns)
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblLocationHistory_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblLocationHistory_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblLocationHistory_DateCreated DEFAULT (GETDATE()),

        CONSTRAINT PK_tblLocationHistory_LocationHistoryId PRIMARY KEY (LocationHistoryId),
        CONSTRAINT FK_tblLocationHistory_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblLocationHistory_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT CK_tblLocationHistory_BatteryLevel
            CHECK (BatteryLevel BETWEEN 0 AND 100)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblLocationHistory_Id' AND object_id = OBJECT_ID(N'dbo.tblLocationHistory'))
BEGIN
    CREATE UNIQUE INDEX UK_tblLocationHistory_Id ON dbo.tblLocationHistory (Id);
END;
GO

-- Last-known location per member (map view + stale detection) + SafetyWorker 30-day purge
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblLocationHistory_FamilyMemberId_RecordedAt' AND object_id = OBJECT_ID(N'dbo.tblLocationHistory'))
BEGIN
    CREATE INDEX IDX_tblLocationHistory_FamilyMemberId_RecordedAt
        ON dbo.tblLocationHistory (FamilyMemberId, RecordedAt DESC);
END;
GO
