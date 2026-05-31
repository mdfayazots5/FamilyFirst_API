-- FlagKey was the original string PK. Per SQL Format, every table requires BIGINT IDENTITY + GUID PK.
-- FlagKey is now a business column with a unique index for fast keyed lookup.
IF OBJECT_ID(N'dbo.tblFeatureFlag', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFeatureFlag
    (
        FeatureFlagId   BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_Id DEFAULT (NEWID()),
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_SiteId DEFAULT (1),
        DepartmentId    INT NULL,

        -- Business columns
        FlagKey         NVARCHAR(128) NOT NULL,
        FlagValue       NVARCHAR(256) NOT NULL,
        Description     NVARCHAR(512) NULL,

        -- Audit columns
        Tag             NVARCHAR(64) NULL,
        Comments        NVARCHAR(256) NULL,
        DisplayOnWeb    BIT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_DisplayOnWeb DEFAULT (1),
        IsPublished     BIT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_IsPublished DEFAULT (1),
        DatePublished   DATETIME2 NULL,
        PublishedBy     NVARCHAR(128) NULL,
        SortOrder       INT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_SortOrder DEFAULT (0),
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_DateCreated DEFAULT (GETDATE()),
        UpdatedBy       NVARCHAR(128) NULL,
        LastUpdated     DATETIME2 NULL,
        DeletedBy       NVARCHAR(128) NULL,
        DateDeleted     DATETIME2 NULL,
        IsDeleted       BIT NOT NULL
                            CONSTRAINT DF_tblFeatureFlag_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblFeatureFlag_FeatureFlagId PRIMARY KEY (FeatureFlagId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFeatureFlag_Id' AND object_id = OBJECT_ID(N'dbo.tblFeatureFlag'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFeatureFlag_Id ON dbo.tblFeatureFlag (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFeatureFlag_FlagKey' AND object_id = OBJECT_ID(N'dbo.tblFeatureFlag'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFeatureFlag_FlagKey ON dbo.tblFeatureFlag (FlagKey) WHERE IsDeleted = 0;
END;
GO
