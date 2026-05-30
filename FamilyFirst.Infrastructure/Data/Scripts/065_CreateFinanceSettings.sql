-- Per-family Finance module configuration: CFO designation + module enabled state.
-- One row per family — enforced via UNIQUE index.
IF OBJECT_ID(N'dbo.FinanceSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinanceSettings
    (
        FinanceSettingId        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FinanceSettings PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        CfoFamilyMemberId       UNIQUEIDENTIFIER NULL,       -- FK → FamilyMembers.FamilyMemberId — designated CFO
        IsModuleEnabled         BIT              NOT NULL CONSTRAINT DF_FinanceSettings_IsModuleEnabled DEFAULT 0,
        EnabledAt               DATETIME2        NULL,
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_FinanceSettings_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_FinanceSettings_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_FinanceSettings_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_FinanceSettings_Families_FamilyId
            FOREIGN KEY (FamilyId)            REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_FinanceSettings_FamilyMembers_CfoFamilyMemberId
            FOREIGN KEY (CfoFamilyMemberId)   REFERENCES dbo.FamilyMembers (FamilyMemberId)
    );
END;
GO

-- One settings row per family
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_FinanceSettings_FamilyId'
      AND object_id = OBJECT_ID(N'dbo.FinanceSettings')
)
BEGIN
    CREATE UNIQUE INDEX UX_FinanceSettings_FamilyId
        ON dbo.FinanceSettings (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
