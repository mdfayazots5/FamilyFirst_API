IF OBJECT_ID(N'dbo.NotificationRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationRules
    (
        RuleId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationRules PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        RuleKey NVARCHAR(50) NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_NotificationRules_IsEnabled DEFAULT 1,
        PriorityOverride INT NULL,
        DeliveryDelayMinutes INT NULL,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotificationRules_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_NotificationRules_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_NotificationRules_FamilyId_RuleKey' AND object_id = OBJECT_ID(N'dbo.NotificationRules'))
BEGIN
    CREATE UNIQUE INDEX UX_NotificationRules_FamilyId_RuleKey
        ON dbo.NotificationRules (FamilyId, RuleKey);
END;
GO
