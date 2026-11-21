-- ============================================================
-- Script  : 111_CreateLookupTables_Level2.sql
-- Purpose : Create the 4 Level 2 simple lookup tables that back the
--           Level 2 MasterDataCodes (FamilyFirst.Domain/Enums/MasterDataCodes.cs):
--             tblDocumentCategory  → DocumentCategory  (20)
--             tblHealthRecordType  → HealthRecordType  (21)
--             tblSafeZoneType      → SafeZoneType      (22)
--             tblFinanceCategory   → FinanceCategory   (23)
--           Structure is identical to 092_CreateLookupTables.sql.
--           <Entity>Code holds the EXACT value persisted today by the
--           Level 2 entities (C# enum name / string constant), so the
--           service layer can map GUID → Code without schema changes.
-- Depends : None
-- Idempotent: OBJECT_ID guard — safe to re-run.
-- ============================================================

-- ── tblDocumentCategory ─────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblDocumentCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblDocumentCategory
    (
        DocumentCategoryId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblDocumentCategory_Id DEFAULT (NEWID()),
        DocumentCategoryName    NVARCHAR(128) NOT NULL,
        DocumentCategoryCode    NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblDocumentCategory_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblDocumentCategory_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblDocumentCategory_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblDocumentCategory_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblDocumentCategory_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblDocumentCategory_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblDocumentCategory_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblDocumentCategory_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblDocumentCategory_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblDocumentCategory_DocumentCategoryId PRIMARY KEY (DocumentCategoryId)
    );
    CREATE UNIQUE INDEX UK_tblDocumentCategory_DocumentCategoryCode ON dbo.tblDocumentCategory (DocumentCategoryCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblHealthRecordType ─────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblHealthRecordType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblHealthRecordType
    (
        HealthRecordTypeId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblHealthRecordType_Id DEFAULT (NEWID()),
        HealthRecordTypeName    NVARCHAR(128) NOT NULL,
        HealthRecordTypeCode    NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblHealthRecordType_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblHealthRecordType_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblHealthRecordType_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblHealthRecordType_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblHealthRecordType_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblHealthRecordType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblHealthRecordType_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblHealthRecordType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblHealthRecordType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblHealthRecordType_HealthRecordTypeId PRIMARY KEY (HealthRecordTypeId)
    );
    CREATE UNIQUE INDEX UK_tblHealthRecordType_HealthRecordTypeCode ON dbo.tblHealthRecordType (HealthRecordTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblSafeZoneType ─────────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblSafeZoneType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSafeZoneType
    (
        SafeZoneTypeId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblSafeZoneType_Id DEFAULT (NEWID()),
        SafeZoneTypeName    NVARCHAR(128) NOT NULL,
        SafeZoneTypeCode    NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblSafeZoneType_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblSafeZoneType_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblSafeZoneType_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblSafeZoneType_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblSafeZoneType_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblSafeZoneType_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblSafeZoneType_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblSafeZoneType_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblSafeZoneType_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblSafeZoneType_SafeZoneTypeId PRIMARY KEY (SafeZoneTypeId)
    );
    CREATE UNIQUE INDEX UK_tblSafeZoneType_SafeZoneTypeCode ON dbo.tblSafeZoneType (SafeZoneTypeCode) WHERE IsDeleted = 0;
END;
GO

-- ── tblFinanceCategory ──────────────────────────────────────────
IF OBJECT_ID(N'dbo.tblFinanceCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFinanceCategory
    (
        FinanceCategoryId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblFinanceCategory_Id DEFAULT (NEWID()),
        FinanceCategoryName    NVARCHAR(128) NOT NULL,
        FinanceCategoryCode    NVARCHAR(64) NOT NULL,
        CompanyId       INT NOT NULL CONSTRAINT DF_tblFinanceCategory_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL CONSTRAINT DF_tblFinanceCategory_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL CONSTRAINT DF_tblFinanceCategory_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL CONSTRAINT DF_tblFinanceCategory_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblFinanceCategory_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL CONSTRAINT DF_tblFinanceCategory_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_tblFinanceCategory_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL CONSTRAINT DF_tblFinanceCategory_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblFinanceCategory_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblFinanceCategory_FinanceCategoryId PRIMARY KEY (FinanceCategoryId)
    );
    CREATE UNIQUE INDEX UK_tblFinanceCategory_FinanceCategoryCode ON dbo.tblFinanceCategory (FinanceCategoryCode) WHERE IsDeleted = 0;
END;
GO
