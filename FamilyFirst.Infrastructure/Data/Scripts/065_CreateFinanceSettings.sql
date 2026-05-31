-- Per-family Finance module configuration: CFO designation + module enabled state.
-- One row per family — enforced via UNIQUE index.
IF OBJECT_ID(N'dbo.tblFinanceSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFinanceSettings
    (
        FinanceSettingsId       BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        -- Designated CFO; nullable until FamilyAdmin assigns one
        CfoFamilyMemberId       BIGINT NULL,
        IsModuleEnabled         BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_IsModuleEnabled DEFAULT (0),
        EnabledAt               DATETIME2 NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceSettings_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblFinanceSettings_FinanceSettingsId PRIMARY KEY (FinanceSettingsId),
        CONSTRAINT FK_tblFinanceSettings_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblFinanceSettings_CfoFamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (CfoFamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFinanceSettings_Id' AND object_id = OBJECT_ID(N'dbo.tblFinanceSettings'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFinanceSettings_Id ON dbo.tblFinanceSettings (Id) WHERE IsDeleted = 0;
END;
GO

-- One settings row per family
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFinanceSettings_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblFinanceSettings'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFinanceSettings_FamilyId
        ON dbo.tblFinanceSettings (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
