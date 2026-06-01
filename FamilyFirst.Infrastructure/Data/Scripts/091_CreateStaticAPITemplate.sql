-- ============================================================
-- Script  : 091_CreateStaticAPITemplate.sql
-- Purpose : Create tblStaticAPITemplate — maps API method names to
--           stored procedures for the generic GetDataByCode and
--           GetDataBySearch endpoints.
--           When the controller receives a ModuleCode, it looks up
--           the SP name from this table and executes it dynamically.
--
-- Reference: RevalPOS_RevalERPlocalDB.dbo.tblStaticAPITemplate
--   StaticAPIMethodName  — e.g. 'GetDataByCode', 'GetDataBySearch', 'Publish'
--   StoredProcedureName  — e.g. 'uspGetAttendanceBySearch'
--   ModuleId             — which FF module this template belongs to
--   StaticAPIMode        — e.g. 'Code', 'Search', 'Publish'
--   StaticAPIType        — e.g. 'GET', 'POST'
--   APITemplateId        — optional link to tblAPITemplate (nullable — FF does not
--                          require tblAPITemplate for Phase A/B)
--
-- Depends : 076_CreateAPIMethod.sql (optional: APIMethodId reference)
--           069_CreateModule.sql    (ModuleId FK)
-- ============================================================

IF OBJECT_ID(N'dbo.tblStaticAPITemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblStaticAPITemplate
    (
        -- Identity columns
        StaticAPITemplateId     BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_Id DEFAULT (NEWID()),

        -- Business columns
        StaticAPIMethodName     NVARCHAR(256) NOT NULL,
        StoredProcedureName     NVARCHAR(512) NOT NULL,
        StaticAPIType           NVARCHAR(128) NULL,
        StaticAPIMode           NVARCHAR(256) NULL,
        APIRequestParametersExplain NVARCHAR(256) NULL,
        APIRequestJson          NVARCHAR(1024) NULL,
        -- Module scope (FK to tblModule — which module owns this template)
        ModuleId                BIGINT NULL,
        -- Optional link to tblAPITemplate (nullable — not required for FF Level 1)
        APITemplateId           BIGINT NULL,

        -- Audit columns
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_SiteId DEFAULT (1),
        DepartmentId            INT NULL,
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblStaticAPITemplate_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblStaticAPITemplate_StaticAPITemplateId
            PRIMARY KEY (StaticAPITemplateId),

        CONSTRAINT FK_tblStaticAPITemplate_ModuleId_tblModule_ModuleId
            FOREIGN KEY (ModuleId) REFERENCES dbo.tblModule (ModuleId)
    );

    CREATE INDEX IDX_tblStaticAPITemplate_ModuleId
        ON dbo.tblStaticAPITemplate (ModuleId)
        WHERE IsDeleted = 0;

    CREATE INDEX IDX_tblStaticAPITemplate_StaticAPIMethodName
        ON dbo.tblStaticAPITemplate (StaticAPIMethodName)
        WHERE IsDeleted = 0;

    -- Composite index for the primary lookup: method name + mode + module
    CREATE UNIQUE INDEX UK_tblStaticAPITemplate_Method_Mode_Module
        ON dbo.tblStaticAPITemplate (StaticAPIMethodName, StaticAPIMode, ModuleId)
        WHERE IsDeleted = 0;
END;
GO

-- ── Seed: FamilyFirst Level 1 Static API Templates ────────────────────────
-- GetDataByCode: UI passes ModuleCode → SP returns dropdown options.
-- GetDataBySearch: UI passes search params → SP returns list with pagination.
-- Seeded for the 10 Level 1 modules. SP names will be created per-module.
-- ModuleId values: AUTH=1, FAMILY=2, DASH=3, ATTEND=4, TASK=5,
--                  FEEDBACK=6, REWARDS=7, CALENDAR=8, NOTIF=9, ADMIN=10

INSERT INTO dbo.tblStaticAPITemplate
    (StaticAPIMethodName, StoredProcedureName, StaticAPIType, StaticAPIMode, ModuleId,
     Comments, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT
    source.StaticAPIMethodName,
    source.StoredProcedureName,
    source.StaticAPIType,
    source.StaticAPIMode,
    source.ModuleId,
    source.Comments,
    1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- Attendance
    (N'GetDataBySearch', N'uspGetAttendanceBySearch',         N'POST', N'Search', 4, N'List attendance records with filters'),
    (N'GetDataByCode',   N'uspGetAttendanceById',             N'POST', N'Code',   4, N'Get single attendance session by GUID'),
    -- Tasks
    (N'GetDataBySearch', N'uspGetTaskItemBySearch',           N'POST', N'Search', 5, N'List task items with filters'),
    (N'GetDataByCode',   N'uspGetTaskItemById',               N'POST', N'Code',   5, N'Get single task item by GUID'),
    -- Feedback
    (N'GetDataBySearch', N'uspGetTeacherFeedbackBySearch',    N'POST', N'Search', 6, N'List teacher feedback with filters'),
    (N'GetDataByCode',   N'uspGetTeacherFeedbackById',        N'POST', N'Code',   6, N'Get single feedback record by GUID'),
    -- Rewards
    (N'GetDataBySearch', N'uspGetRewardBySearch',             N'POST', N'Search', 7, N'List rewards with filters'),
    (N'GetDataByCode',   N'uspGetRewardById',                 N'POST', N'Code',   7, N'Get single reward by GUID'),
    -- Calendar
    (N'GetDataBySearch', N'uspGetCalendarEventBySearch',      N'POST', N'Search', 8, N'List calendar events with filters'),
    (N'GetDataByCode',   N'uspGetCalendarEventById',          N'POST', N'Code',   8, N'Get single calendar event by GUID'),
    -- Notifications
    (N'GetDataBySearch', N'uspGetNotificationBySearch',       N'POST', N'Search', 9, N'List notifications with filters'),
    -- Master Data (all modules share this)
    (N'GetDataByCode',   N'uspGetMasterDataByCode',           N'POST', N'Code',   NULL, N'Generic master data dropdown lookup')
) AS source (StaticAPIMethodName, StoredProcedureName, StaticAPIType, StaticAPIMode, ModuleId, Comments)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblStaticAPITemplate t
    WHERE t.StaticAPIMethodName = source.StaticAPIMethodName
      AND t.StaticAPIMode       = source.StaticAPIMode
      AND (
            (t.ModuleId IS NULL AND source.ModuleId IS NULL)
            OR t.ModuleId = source.ModuleId
          )
      AND t.IsDeleted = 0
);
GO
