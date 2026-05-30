-- Tracks PDF/Image export jobs. For MVP, exports are synchronous (< 5s via QuestPDF).
-- Row records the job status, S3 download URL (15-min pre-signed), and expiry.
IF OBJECT_ID(N'dbo.ReportExports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportExports
    (
        ReportExportId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportExports PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        RequestedByUserId       UNIQUEIDENTIFIER NOT NULL,
        -- ReportType: WeeklyDigest / MonthlyFamily / ChildMonthly / Finance / AttendanceSummary
        ReportType              NVARCHAR(30)     NOT NULL,
        Period                  NVARCHAR(10)     NOT NULL,
        ChildId                 UNIQUEIDENTIFIER NULL,
        -- Format: PDF / Image
        Format                  NVARCHAR(10)     NOT NULL,
        -- Status: Processing / Ready / Failed
        Status                  NVARCHAR(20)     NOT NULL CONSTRAINT DF_ReportExports_Status DEFAULT N'Processing',
        DownloadUrl             NVARCHAR(1000)   NULL,
        ExpiresAtUtc            DATETIME2        NULL,
        ErrorMessage            NVARCHAR(500)    NULL,
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_ReportExports_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_ReportExports_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_ReportExports_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_ReportExports_Families_FamilyId
            FOREIGN KEY (FamilyId)            REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_ReportExports_Users_RequestedByUserId
            FOREIGN KEY (RequestedByUserId)   REFERENCES dbo.Users    (UserId),
        CONSTRAINT FK_ReportExports_ChildProfiles_ChildId
            FOREIGN KEY (ChildId)             REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT CK_ReportExports_ReportType
            CHECK (ReportType IN (N'WeeklyDigest', N'MonthlyFamily', N'ChildMonthly', N'Finance', N'AttendanceSummary')),
        CONSTRAINT CK_ReportExports_Format
            CHECK (Format IN (N'PDF', N'Image')),
        CONSTRAINT CK_ReportExports_Status
            CHECK (Status IN (N'Processing', N'Ready', N'Failed'))
    );
END;
GO

-- Recent exports per family — for re-download within 15-min URL validity window
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ReportExports_FamilyId_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.ReportExports')
)
BEGIN
    CREATE INDEX IX_ReportExports_FamilyId_CreatedAt
        ON dbo.ReportExports (FamilyId, CreatedAt DESC)
        WHERE IsDeleted = 0;
END;
GO
