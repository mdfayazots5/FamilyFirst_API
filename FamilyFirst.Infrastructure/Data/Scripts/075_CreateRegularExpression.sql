-- ============================================================
-- Script  : 075_CreateRegularExpression.sql
-- Purpose : Create tblRegularExpression — per-API, per-field
--           regex patterns. Loaded into IMemoryCache at startup.
--           BAL uses cached regex instead of hardcoded patterns.
--           NOTE: This table does not exist in the reference DB
--           (founder's notes have a typo: tblregularexpresion).
--           This is a new table created correctly for FamilyFirst.
-- Depends : 076_CreateAPIMethod.sql (FK added in script 076)
-- ============================================================

IF OBJECT_ID(N'dbo.tblRegularExpression', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRegularExpression
    (
        -- Identity columns
        RegularExpressionId BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblRegularExpression_Id DEFAULT (NEWID()),

        -- Business columns
        -- APIMethodId FK added by 076_CreateAPIMethod.sql
        APIMethodId         BIGINT NULL,
        FieldName           NVARCHAR(128) NOT NULL,
        RegexPattern        NVARCHAR(1024) NOT NULL,
        Description         NVARCHAR(512) NULL,

        -- Audit columns
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblRegularExpression_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblRegularExpression_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblRegularExpression_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblRegularExpression_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblRegularExpression_RegularExpressionId PRIMARY KEY (RegularExpressionId)
    );

    -- Lookup by method + field (used by cache warmup service)
    CREATE INDEX IDX_tblRegularExpression_APIMethodId_FieldName
        ON dbo.tblRegularExpression (APIMethodId, FieldName)
        WHERE IsDeleted = 0;
END;
GO
