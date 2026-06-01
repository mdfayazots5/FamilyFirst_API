-- ============================================================
-- Script  : 069_CreateModule.sql
-- Purpose : Create tblModule — FamilyFirst Level 1 module registry.
--           10 modules: AUTH, FAMILY, DASH, ATTEND, TASK, FEEDBACK,
--           REWARDS, CALENDAR, NOTIF, ADMIN.
--           ModuleId INT values match the Module enum in FamilyFirstEnums.cs.
-- Depends : None
-- ============================================================

IF OBJECT_ID(N'dbo.tblModule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblModule
    (
        -- Identity columns
        ModuleId        BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblModule_Id DEFAULT (NEWID()),

        -- Business columns
        ModuleName      NVARCHAR(128) NOT NULL,
        ModuleCode      NVARCHAR(64) NOT NULL,
        ParentModuleId  BIGINT NULL,

        -- Audit columns
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblModule_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblModule_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblModule_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblModule_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblModule_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblModule_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblModule_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblModule_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblModule_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblModule_ModuleId PRIMARY KEY (ModuleId)
    );

    CREATE UNIQUE INDEX UK_tblModule_ModuleCode
        ON dbo.tblModule (ModuleCode)
        WHERE IsDeleted = 0;
END;
GO
