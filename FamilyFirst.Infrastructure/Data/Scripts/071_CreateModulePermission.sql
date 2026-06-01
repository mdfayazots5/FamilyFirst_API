-- ============================================================
-- Script  : 071_CreateModulePermission.sql
-- Purpose : Create tblModulePermission — maps which permission
--           types are applicable to each module.
--           E.g. Attendance supports CU + AR; Calendar supports V + CU.
-- Depends : 069_CreateModule.sql, 067_CreatePermission.sql
-- ============================================================

IF OBJECT_ID(N'dbo.tblModulePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblModulePermission
    (
        -- Identity columns
        ModulePermissionId  BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblModulePermission_Id DEFAULT (NEWID()),

        -- Business columns
        ModuleId            BIGINT NOT NULL,
        PermissionId        BIGINT NOT NULL,

        -- Audit columns
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblModulePermission_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblModulePermission_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblModulePermission_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblModulePermission_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblModulePermission_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblModulePermission_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblModulePermission_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblModulePermission_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblModulePermission_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblModulePermission_ModulePermissionId PRIMARY KEY (ModulePermissionId),
        CONSTRAINT FK_tblModulePermission_ModuleId_tblModule_ModuleId
            FOREIGN KEY (ModuleId) REFERENCES dbo.tblModule (ModuleId),
        CONSTRAINT FK_tblModulePermission_PermissionId_tblPermission_PermissionId
            FOREIGN KEY (PermissionId) REFERENCES dbo.tblPermission (PermissionId)
    );

    CREATE INDEX IDX_tblModulePermission_ModuleId
        ON dbo.tblModulePermission (ModuleId);

    CREATE UNIQUE INDEX UK_tblModulePermission_ModuleId_PermissionId
        ON dbo.tblModulePermission (ModuleId, PermissionId)
        WHERE IsDeleted = 0;
END;
GO
