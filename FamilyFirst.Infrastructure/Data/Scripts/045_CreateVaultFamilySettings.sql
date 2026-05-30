-- Per-family vault configuration: emergency access mode and emergency PIN.
-- One row per family. Inserted on first vault access; updated by FamilyAdmin.
IF OBJECT_ID(N'dbo.VaultFamilySettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaultFamilySettings
    (
        SettingsId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VaultFamilySettings PRIMARY KEY DEFAULT NEWID(),
        FamilyId              UNIQUEIDENTIFIER NOT NULL,
        -- EmergencyAccessMode: 1=LoginRequired, 2=PinOnly, 3=NoLogin
        EmergencyAccessMode   INT              NOT NULL CONSTRAINT DF_VaultFamilySettings_EmergencyAccessMode DEFAULT 1,
        EmergencyPinHash      NVARCHAR(200)    NULL,
        CreatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultFamilySettings_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultFamilySettings_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted             BIT              NOT NULL CONSTRAINT DF_VaultFamilySettings_IsDeleted DEFAULT 0,
        DeletedAt             DATETIME2        NULL,

        CONSTRAINT FK_VaultFamilySettings_Families_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_VaultFamilySettings_EmergencyAccessMode
            CHECK (EmergencyAccessMode BETWEEN 1 AND 3)
    );
END;
GO

-- One settings row per family — enforced at DB level
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultFamilySettings_FamilyId'
      AND object_id = OBJECT_ID(N'dbo.VaultFamilySettings')
)
BEGIN
    CREATE UNIQUE INDEX IX_VaultFamilySettings_FamilyId
        ON dbo.VaultFamilySettings (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
