-- 12-month archive of generated weekly digests.
-- WeeklyDigestWorker inserts one row per family per Sunday; auto-purges rows older than 12 months.
IF OBJECT_ID(N'dbo.WeeklyDigestArchive', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WeeklyDigestArchive
    (
        WeeklyDigestArchiveId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WeeklyDigestArchive PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        WeekStartDate           DATE             NOT NULL,
        DigestContentJson       NVARCHAR(MAX)    NOT NULL,
        GeneratedAt             DATETIME2        NOT NULL,
        ShareableImageUrl       NVARCHAR(1000)   NULL,
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_WeeklyDigestArchive_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_WeeklyDigestArchive_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_WeeklyDigestArchive_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_WeeklyDigestArchive_Families_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_WeeklyDigestArchive_FamilyId_WeekStartDate'
      AND object_id = OBJECT_ID(N'dbo.WeeklyDigestArchive')
)
BEGIN
    CREATE UNIQUE INDEX UX_WeeklyDigestArchive_FamilyId_WeekStartDate
        ON dbo.WeeklyDigestArchive (FamilyId, WeekStartDate)
        WHERE IsDeleted = 0;
END;
GO
