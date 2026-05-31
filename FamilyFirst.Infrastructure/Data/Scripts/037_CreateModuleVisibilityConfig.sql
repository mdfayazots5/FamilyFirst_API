IF OBJECT_ID(N'dbo.tblModuleVisibilityConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblModuleVisibilityConfig
    (
        ModuleVisibilityConfigId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        FamilyId                    BIGINT NULL,
        RoleId                      INT NOT NULL,
        ModuleName                  NVARCHAR(128) NOT NULL,
        IsVisible                   BIT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_IsVisible DEFAULT (1),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblModuleVisibilityConfig_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblModuleVisibilityConfig_ModuleVisibilityConfigId
            PRIMARY KEY (ModuleVisibilityConfigId),
        CONSTRAINT FK_tblModuleVisibilityConfig_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblModuleVisibilityConfig_Id' AND object_id = OBJECT_ID(N'dbo.tblModuleVisibilityConfig'))
BEGIN
    CREATE UNIQUE INDEX UK_tblModuleVisibilityConfig_Id
        ON dbo.tblModuleVisibilityConfig (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblModuleVisibilityConfig_FamilyId_RoleId_ModuleName' AND object_id = OBJECT_ID(N'dbo.tblModuleVisibilityConfig'))
BEGIN
    CREATE UNIQUE INDEX UK_tblModuleVisibilityConfig_FamilyId_RoleId_ModuleName
        ON dbo.tblModuleVisibilityConfig (FamilyId, RoleId, ModuleName);
END;
GO
