-- ============================================================
-- Script  : 067_CreatePermission.sql
-- Purpose : Create tblPermission — stores the 5 operation-level
--           permission types used for fine-grained BAL access control.
-- Depends : None
-- Run After: 066_AlterVaultFamilySettings_AddAdminConfig.sql
-- ============================================================

IF OBJECT_ID(N'dbo.tblPermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblPermission
    (
        -- Identity columns
        PermissionId    BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblPermission_Id DEFAULT (NEWID()),

        -- Business columns
        PermissionName  NVARCHAR(128) NOT NULL,
        PermissionCode  NVARCHAR(64) NOT NULL,

        -- Audit columns
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblPermission_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblPermission_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblPermission_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblPermission_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblPermission_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblPermission_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblPermission_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblPermission_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblPermission_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblPermission_PermissionId PRIMARY KEY (PermissionId)
    );

    CREATE UNIQUE INDEX UK_tblPermission_PermissionCode
        ON dbo.tblPermission (PermissionCode)
        WHERE IsDeleted = 0;
END;
GO
