-- ============================================================
-- Script  : 076_CreateAPIMethod.sql
-- Purpose : Create tblAPIMethod — registry of all API endpoints.
--           Used for: async API logging, rate limiting, regex
--           pattern lookup by method ID, and cache warmup.
--           Also adds FK from tblRegularExpression → tblAPIMethod.
-- Depends : 075_CreateRegularExpression.sql (adds FK back to it)
-- ============================================================

IF OBJECT_ID(N'dbo.tblAPIMethod', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAPIMethod
    (
        -- Identity columns
        APIMethodId         BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblAPIMethod_Id DEFAULT (NEWID()),

        -- Business columns
        MethodName          NVARCHAR(128) NOT NULL,
        APIURL              NVARCHAR(512) NULL,
        HTTPMethod          NVARCHAR(16) NULL,
        ContentType         NVARCHAR(128) NULL
                                CONSTRAINT DF_tblAPIMethod_ContentType DEFAULT (N'application/json'),
        RequestMaxCount     BIGINT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_RequestMaxCount DEFAULT (100),
        RequestTimeSpan     BIGINT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_RequestTimeSpan DEFAULT (3600),

        -- Audit columns
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(512) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblAPIMethod_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblAPIMethod_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblAPIMethod_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblAPIMethod_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblAPIMethod_APIMethodId PRIMARY KEY (APIMethodId)
    );

    CREATE UNIQUE INDEX UK_tblAPIMethod_MethodName
        ON dbo.tblAPIMethod (MethodName)
        WHERE IsDeleted = 0;
END;
GO

-- Add FK from tblRegularExpression to tblAPIMethod (deferred from script 075)
IF OBJECT_ID(N'dbo.tblRegularExpression', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.tblAPIMethod', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_tblRegularExpression_APIMethodId_tblAPIMethod_APIMethodId'
    )
BEGIN
    ALTER TABLE dbo.tblRegularExpression
    ADD CONSTRAINT FK_tblRegularExpression_APIMethodId_tblAPIMethod_APIMethodId
        FOREIGN KEY (APIMethodId) REFERENCES dbo.tblAPIMethod (APIMethodId);
END;
GO
