-- Tracks PDF/Image export jobs. For MVP, exports are synchronous (< 5s via QuestPDF).
-- Row records the job status, S3 download URL (15-min pre-signed), and expiry.
IF OBJECT_ID(N'dbo.tblReportExport', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblReportExport
    (
        ReportExportId          BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblReportExport_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblReportExport_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblReportExport_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        RequestedByUserId       BIGINT NOT NULL,
        -- ReportType: WeeklyDigest / MonthlyFamily / ChildMonthly / Finance / AttendanceSummary
        ReportType              NVARCHAR(32) NOT NULL,
        Period                  NVARCHAR(16) NOT NULL,
        ChildProfileId          BIGINT NULL,
        -- Format: PDF / Image
        Format                  NVARCHAR(16) NOT NULL,
        -- Status: Processing / Ready / Failed
        Status                  NVARCHAR(24) NOT NULL
                                    CONSTRAINT DF_tblReportExport_Status DEFAULT (N'Processing'),
        DownloadUrl             NVARCHAR(1024) NULL,
        ExpiresAtUtc            DATETIME2 NULL,
        ErrorMessage            NVARCHAR(512) NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblReportExport_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblReportExport_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblReportExport_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblReportExport_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblReportExport_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblReportExport_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblReportExport_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblReportExport_ReportExportId PRIMARY KEY (ReportExportId),
        CONSTRAINT FK_tblReportExport_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblReportExport_RequestedByUserId_tblUser_UserId
            FOREIGN KEY (RequestedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblReportExport_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT CK_tblReportExport_ReportType
            CHECK (ReportType IN (N'WeeklyDigest', N'MonthlyFamily', N'ChildMonthly', N'Finance', N'AttendanceSummary')),
        CONSTRAINT CK_tblReportExport_Format
            CHECK (Format IN (N'PDF', N'Image')),
        CONSTRAINT CK_tblReportExport_Status
            CHECK (Status IN (N'Processing', N'Ready', N'Failed'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblReportExport_Id' AND object_id = OBJECT_ID(N'dbo.tblReportExport'))
BEGIN
    CREATE UNIQUE INDEX UK_tblReportExport_Id ON dbo.tblReportExport (Id) WHERE IsDeleted = 0;
END;
GO

-- Recent exports per family — for re-download within 15-min URL validity window
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblReportExport_FamilyId_DateCreated' AND object_id = OBJECT_ID(N'dbo.tblReportExport'))
BEGIN
    CREATE INDEX IDX_tblReportExport_FamilyId_DateCreated
        ON dbo.tblReportExport (FamilyId, DateCreated DESC)
        WHERE IsDeleted = 0;
END;
GO
