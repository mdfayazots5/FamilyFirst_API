-- Monthly snapshot of ChildProfile pillar scores.
-- Required for RP-04 3-month radar chart evolution overlay.
-- WeeklyDigestWorker inserts one row per child on the first Sunday of each month.
-- Auto-purges rows older than 13 months. IsDeleted/UpdatedBy/DeletedBy omitted — append-only.
IF OBJECT_ID(N'dbo.tblChildPillarScoreHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblChildPillarScoreHistory
    (
        ChildPillarScoreHistoryId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_SiteId DEFAULT (1),

        -- Business columns
        ChildProfileId              BIGINT NOT NULL,
        FamilyId                    BIGINT NOT NULL,
        -- First day of the snapshot month: e.g. 2026-04-01
        SnapshotMonth               DATETIME2 NOT NULL,
        StudyScore                  INT NOT NULL,
        CleanlinessScore            INT NOT NULL,
        DisciplineScore             INT NOT NULL,
        ScreenControlScore          INT NOT NULL,
        ResponsibilityScore         INT NOT NULL,

        -- Minimal audit columns (append-only — no update or delete columns)
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblChildPillarScoreHistory_DateCreated DEFAULT (GETDATE()),

        CONSTRAINT PK_tblChildPillarScoreHistory_ChildPillarScoreHistoryId
            PRIMARY KEY (ChildPillarScoreHistoryId),
        CONSTRAINT FK_tblChildPillarScoreHistory_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblChildPillarScoreHistory_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblChildPillarScoreHistory_Id' AND object_id = OBJECT_ID(N'dbo.tblChildPillarScoreHistory'))
BEGIN
    CREATE UNIQUE INDEX UK_tblChildPillarScoreHistory_Id ON dbo.tblChildPillarScoreHistory (Id);
END;
GO

-- One snapshot per child per month
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblChildPillarScoreHistory_ChildProfileId_SnapshotMonth' AND object_id = OBJECT_ID(N'dbo.tblChildPillarScoreHistory'))
BEGIN
    CREATE UNIQUE INDEX UK_tblChildPillarScoreHistory_ChildProfileId_SnapshotMonth
        ON dbo.tblChildPillarScoreHistory (ChildProfileId, SnapshotMonth);
END;
GO

-- Family-scoped 3-month lookback for RP-04 (also used for purge by family)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblChildPillarScoreHistory_FamilyId_SnapshotMonth' AND object_id = OBJECT_ID(N'dbo.tblChildPillarScoreHistory'))
BEGIN
    CREATE INDEX IDX_tblChildPillarScoreHistory_FamilyId_SnapshotMonth
        ON dbo.tblChildPillarScoreHistory (FamilyId, SnapshotMonth DESC);
END;
GO
