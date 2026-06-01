-- ============================================================
-- Script  : 070_CreateSubModule.sql
-- Purpose : Create tblSubModule — sub-module groupings within
--           each Level 1 module. Used for fine-grained navigation
--           and optional per-sub-module permission overrides.
-- Depends : 069_CreateModule.sql (tblModule)
-- ============================================================

IF OBJECT_ID(N'dbo.tblSubModule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSubModule
    (
        -- Identity columns
        SubModuleId     BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblSubModule_Id DEFAULT (NEWID()),

        -- Business columns
        ModuleId        BIGINT NOT NULL,
        SubModuleName   NVARCHAR(128) NOT NULL,
        SubModuleCode   NVARCHAR(64) NOT NULL,

        -- Audit columns
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblSubModule_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblSubModule_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblSubModule_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblSubModule_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblSubModule_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblSubModule_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblSubModule_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblSubModule_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblSubModule_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblSubModule_SubModuleId PRIMARY KEY (SubModuleId),
        CONSTRAINT FK_tblSubModule_ModuleId_tblModule_ModuleId
            FOREIGN KEY (ModuleId) REFERENCES dbo.tblModule (ModuleId)
    );

    CREATE INDEX IDX_tblSubModule_ModuleId
        ON dbo.tblSubModule (ModuleId);
END;
GO
