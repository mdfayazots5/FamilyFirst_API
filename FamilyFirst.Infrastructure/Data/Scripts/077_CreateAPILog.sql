-- ============================================================
-- Script  : 077_CreateAPILog.sql
-- Purpose : Create tblAPILog — async API request/response log.
--           Every service method calls uspInsertAPILog via
--           Task.Run (fire-and-forget) — does NOT block HTTP response.
--           Simplified from reference tblAPILogDetail: no POS-specific
--           fields (PoolingIn, AccrualPoints, SiteCode removed).
-- Depends : 076_CreateAPIMethod.sql (optional FK)
-- ============================================================

IF OBJECT_ID(N'dbo.tblAPILog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAPILog
    (
        -- Identity columns
        APILogId            BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblAPILog_Id DEFAULT (NEWID()),

        -- Business columns
        APIMethodId         BIGINT NULL,
        MethodName          NVARCHAR(256) NULL,
        RequestJSON         NVARCHAR(MAX) NULL,
        ResponseJSON        NVARCHAR(MAX) NULL,
        Token               NVARCHAR(2048) NULL,
        CreatedByUserId     BIGINT NULL,

        -- Audit columns (no IsDeleted — log records are never soft-deleted)
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblAPILog_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblAPILog_SiteId DEFAULT (1),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblAPILog_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblAPILog_CreatedBy DEFAULT (N'System'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblAPILog_DateCreated DEFAULT (GETDATE()),

        CONSTRAINT PK_tblAPILog_APILogId PRIMARY KEY (APILogId)
    );

    -- Lookup by method for log browsing
    CREATE INDEX IDX_tblAPILog_APIMethodId
        ON dbo.tblAPILog (APIMethodId);

    CREATE INDEX IDX_tblAPILog_CreatedByUserId
        ON dbo.tblAPILog (CreatedByUserId);

    CREATE INDEX IDX_tblAPILog_DateCreated
        ON dbo.tblAPILog (DateCreated DESC);
END;
GO
