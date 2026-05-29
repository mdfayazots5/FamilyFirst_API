IF OBJECT_ID(N'dbo.ModuleVisibilityConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModuleVisibilityConfig
    (
        ConfigId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ModuleVisibilityConfig PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NULL,
        RoleId INT NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        IsVisible BIT NOT NULL CONSTRAINT DF_ModuleVisibilityConfig_IsVisible DEFAULT 1,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ModuleVisibilityConfig_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ModuleVisibilityConfig_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ModuleVisibilityConfig_FamilyId_RoleId_ModuleName' AND object_id = OBJECT_ID(N'dbo.ModuleVisibilityConfig'))
BEGIN
    CREATE UNIQUE INDEX UX_ModuleVisibilityConfig_FamilyId_RoleId_ModuleName
        ON dbo.ModuleVisibilityConfig (FamilyId, RoleId, ModuleName);
END;
GO
