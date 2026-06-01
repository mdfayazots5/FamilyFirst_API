-- ============================================================
-- Script  : 074_CreateErrorCode.sql
-- Purpose : Create tblErrorCode — DB-driven error messages.
--           BAL reads these in the finally block instead of
--           hardcoded strings. Supports future multilingual
--           messages via LanguageId column.
-- Depends : None
-- ============================================================

IF OBJECT_ID(N'dbo.tblErrorCode', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblErrorCode
    (
        -- Identity columns
        ErrorCodeId     BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblErrorCode_Id DEFAULT (NEWID()),

        -- Business columns
        ErrorCode       INT NOT NULL,
        ErrorName       NVARCHAR(256) NULL,
        ReturnCode      INT NOT NULL,
        ReturnMessage   NVARCHAR(1024) NOT NULL,
        LanguageId      INT NOT NULL
                            CONSTRAINT DF_tblErrorCode_LanguageId DEFAULT (1),

        -- Audit columns
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblErrorCode_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblErrorCode_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblErrorCode_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblErrorCode_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblErrorCode_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblErrorCode_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblErrorCode_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblErrorCode_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblErrorCode_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblErrorCode_ErrorCodeId PRIMARY KEY (ErrorCodeId)
    );

    -- Primary lookup: ErrorCode + LanguageId
    CREATE UNIQUE INDEX UK_tblErrorCode_ErrorCode_LanguageId
        ON dbo.tblErrorCode (ErrorCode, LanguageId)
        WHERE IsDeleted = 0;
END;
GO
