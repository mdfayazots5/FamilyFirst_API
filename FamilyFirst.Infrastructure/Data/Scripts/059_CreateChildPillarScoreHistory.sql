-- Monthly snapshot of ChildProfile pillar scores.
-- Required for RP-04 3-month radar chart evolution overlay.
-- WeeklyDigestWorker inserts one row per child on the first Sunday of each month.
-- Auto-purges rows older than 13 months (keeps 12 full months + current month in progress).
IF OBJECT_ID(N'dbo.ChildPillarScoreHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChildPillarScoreHistory
    (
        ChildPillarScoreHistoryId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChildPillarScoreHistory PRIMARY KEY DEFAULT NEWID(),
        ChildProfileId              UNIQUEIDENTIFIER NOT NULL,
        FamilyId                    UNIQUEIDENTIFIER NOT NULL,
        -- First day of the snapshot month: e.g. 2026-04-01
        SnapshotMonth               DATE             NOT NULL,
        StudyScore                  INT              NOT NULL,
        CleanlinessScore            INT              NOT NULL,
        DisciplineScore             INT              NOT NULL,
        ScreenControlScore          INT              NOT NULL,
        ResponsibilityScore         INT              NOT NULL,
        CreatedAt                   DATETIME2        NOT NULL CONSTRAINT DF_ChildPillarScoreHistory_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_ChildPillarScoreHistory_ChildProfiles_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_ChildPillarScoreHistory_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId)
    );
END;
GO

-- One snapshot per child per month
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_ChildPillarScoreHistory_ChildProfileId_SnapshotMonth'
      AND object_id = OBJECT_ID(N'dbo.ChildPillarScoreHistory')
)
BEGIN
    CREATE UNIQUE INDEX UX_ChildPillarScoreHistory_ChildProfileId_SnapshotMonth
        ON dbo.ChildPillarScoreHistory (ChildProfileId, SnapshotMonth);
END;
GO

-- Family-scoped 3-month lookback for RP-04 (also used for purge by family)
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ChildPillarScoreHistory_FamilyId_SnapshotMonth'
      AND object_id = OBJECT_ID(N'dbo.ChildPillarScoreHistory')
)
BEGIN
    CREATE INDEX IX_ChildPillarScoreHistory_FamilyId_SnapshotMonth
        ON dbo.ChildPillarScoreHistory (FamilyId, SnapshotMonth DESC);
END;
GO
