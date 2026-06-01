-- ============================================================
-- Script  : 072_CreateRolePermission.sql
-- Purpose : Create tblRolePermission — defines which roles can
--           perform which operations on which modules.
--           Checked by uspCheckRolePermission in every BAL method.
-- Depends : 067_CreatePermission.sql
--           068_CreateRole.sql
--           069_CreateModule.sql
-- ============================================================

IF OBJECT_ID(N'dbo.tblRolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRolePermission
    (
        -- Identity columns
        RolePermissionId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblRolePermission_Id DEFAULT (NEWID()),

        -- Business columns
        RoleId              BIGINT NOT NULL,
        ModuleId            BIGINT NOT NULL,
        PermissionId        BIGINT NOT NULL,

        -- Audit columns
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblRolePermission_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblRolePermission_SiteId DEFAULT (1),
        DepartmentId        INT NULL,
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblRolePermission_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblRolePermission_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblRolePermission_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblRolePermission_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblRolePermission_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblRolePermission_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblRolePermission_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblRolePermission_RolePermissionId PRIMARY KEY (RolePermissionId),
        CONSTRAINT FK_tblRolePermission_RoleId_tblRole_RoleId
            FOREIGN KEY (RoleId) REFERENCES dbo.tblRole (RoleId),
        CONSTRAINT FK_tblRolePermission_ModuleId_tblModule_ModuleId
            FOREIGN KEY (ModuleId) REFERENCES dbo.tblModule (ModuleId),
        CONSTRAINT FK_tblRolePermission_PermissionId_tblPermission_PermissionId
            FOREIGN KEY (PermissionId) REFERENCES dbo.tblPermission (PermissionId)
    );

    CREATE INDEX IDX_tblRolePermission_RoleId
        ON dbo.tblRolePermission (RoleId);

    CREATE INDEX IDX_tblRolePermission_ModuleId
        ON dbo.tblRolePermission (ModuleId);

    CREATE INDEX IDX_tblRolePermission_RoleId_ModuleId
        ON dbo.tblRolePermission (RoleId, ModuleId)
        WHERE IsDeleted = 0;

    CREATE UNIQUE INDEX UK_tblRolePermission_Role_Module_Permission
        ON dbo.tblRolePermission (RoleId, ModuleId, PermissionId)
        WHERE IsDeleted = 0;
END;
GO
