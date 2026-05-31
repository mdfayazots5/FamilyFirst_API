-- 12-month archive of generated weekly digests.
-- WeeklyDigestWorker inserts one row per family per Sunday; auto-purges rows older than 12 months.
-- DigestContentJson uses NVARCHAR(MAX) — justified: arbitrary JSON digest payload of variable size.
IF OBJECT_ID(N'dbo.tblWeeklyDigestArchive', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblWeeklyDigestArchive
    (
        WeeklyDigestArchiveId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        WeekStartDate           DATETIME2 NOT NULL,
        DigestContentJson       NVARCHAR(MAX) NOT NULL,
        GeneratedAt             DATETIME2 NOT NULL,
        ShareableImageUrl       NVARCHAR(1024) NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblWeeklyDigestArchive_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblWeeklyDigestArchive_WeeklyDigestArchiveId PRIMARY KEY (WeeklyDigestArchiveId),
        CONSTRAINT FK_tblWeeklyDigestArchive_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblWeeklyDigestArchive_Id' AND object_id = OBJECT_ID(N'dbo.tblWeeklyDigestArchive'))
BEGIN
    CREATE UNIQUE INDEX UK_tblWeeklyDigestArchive_Id
        ON dbo.tblWeeklyDigestArchive (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblWeeklyDigestArchive_FamilyId_WeekStartDate' AND object_id = OBJECT_ID(N'dbo.tblWeeklyDigestArchive'))
BEGIN
    CREATE UNIQUE INDEX UK_tblWeeklyDigestArchive_FamilyId_WeekStartDate
        ON dbo.tblWeeklyDigestArchive (FamilyId, WeekStartDate)
        WHERE IsDeleted = 0;
END;
GO
