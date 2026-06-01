-- ============================================================
-- Script  : 073_CreateMasterData.sql
-- Purpose : Create tblMasterData — single source of truth for
--           all dropdown/lookup values.
--           UI receives GUID (Id) only — NEVER the INT PK.
--           On save, UI sends GUID → BAL validates via
--           uspGetMasterDataByCodeInternal → gets INT PK for SP.
-- Depends : None (optional FK to tblModule)
-- ============================================================

IF OBJECT_ID(N'dbo.tblMasterData', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblMasterData
    (
        -- Identity columns
        MasterDataId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblMasterData_Id DEFAULT (NEWID()),

        -- Business columns
        MasterDataName      NVARCHAR(128) NOT NULL,
        MasterDataCode      NVARCHAR(64) NOT NULL,
        IsMasterData        BIT NOT NULL
                                CONSTRAINT DF_tblMasterData_IsMasterData DEFAULT (1),
        MasterCodeSpName    NVARCHAR(256) NULL,
        ModuleId            BIGINT NULL,

        -- Audit columns
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblMasterData_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblMasterData_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblMasterData_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblMasterData_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblMasterData_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblMasterData_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblMasterData_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblMasterData_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblMasterData_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblMasterData_MasterDataId PRIMARY KEY (MasterDataId)
    );

    CREATE INDEX IDX_tblMasterData_MasterDataCode
        ON dbo.tblMasterData (MasterDataCode)
        WHERE IsDeleted = 0;

    -- GUID lookup used by uspGetMasterDataByCodeInternal
    CREATE INDEX IDX_tblMasterData_MasterDataCode_Id
        ON dbo.tblMasterData (MasterDataCode, Id)
        WHERE IsDeleted = 0 AND IsPublished = 1;
END;
GO
