-- Per-family vault configuration: emergency access mode and emergency PIN.
-- One row per family. Inserted on first vault access; updated by FamilyAdmin.
IF OBJECT_ID(N'dbo.tblVaultFamilySettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaultFamilySettings
    (
        VaultFamilySettingsId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        -- EmergencyAccessMode: 1=LoginRequired, 2=PinOnly, 3=NoLogin
        EmergencyAccessMode     INT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_EmergencyAccessMode DEFAULT (1),
        EmergencyPinHash        NVARCHAR(256) NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblVaultFamilySettings_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaultFamilySettings_VaultFamilySettingsId PRIMARY KEY (VaultFamilySettingsId),
        CONSTRAINT FK_tblVaultFamilySettings_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblVaultFamilySettings_EmergencyAccessMode
            CHECK (EmergencyAccessMode BETWEEN 1 AND 3)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultFamilySettings_Id' AND object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultFamilySettings_Id
        ON dbo.tblVaultFamilySettings (Id) WHERE IsDeleted = 0;
END;
GO

-- One settings row per family — enforced at DB level
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultFamilySettings_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultFamilySettings_FamilyId
        ON dbo.tblVaultFamilySettings (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
