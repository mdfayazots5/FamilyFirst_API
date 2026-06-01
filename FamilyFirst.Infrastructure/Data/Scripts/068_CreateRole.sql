-- ============================================================
-- Script  : 068_CreateRole.sql
-- Purpose : Create tblRole — FamilyFirst role definitions.
--           Stores the 6 canonical roles: SuperAdmin, FamilyAdmin,
--           Parent, Child, Teacher, Elder.
--           RoleId INT values match the enum in FamilyFirstEnums.cs.
-- Depends : None
-- ============================================================

IF OBJECT_ID(N'dbo.tblRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRole
    (
        -- Identity columns
        RoleId          BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblRole_Id DEFAULT (NEWID()),

        -- Business columns
        RoleName        NVARCHAR(128) NOT NULL,
        RoleCode        NVARCHAR(64) NOT NULL,
        RoleDescription NVARCHAR(512) NULL,

        -- Audit columns
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblRole_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblRole_SiteId DEFAULT (1),
        DepartmentId    INT NULL,
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblRole_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblRole_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblRole_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblRole_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblRole_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblRole_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblRole_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblRole_RoleId PRIMARY KEY (RoleId)
    );

    CREATE UNIQUE INDEX UK_tblRole_RoleCode
        ON dbo.tblRole (RoleCode)
        WHERE IsDeleted = 0;
END;
GO
