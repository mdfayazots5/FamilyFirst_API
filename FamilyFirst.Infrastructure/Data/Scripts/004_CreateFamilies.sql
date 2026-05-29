IF OBJECT_ID(N'dbo.Families', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Families
    (
        FamilyId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Families PRIMARY KEY DEFAULT NEWID(),
        FamilyName NVARCHAR(200) NOT NULL,
        JoinCode NVARCHAR(10) NOT NULL,
        City NVARCHAR(100) NULL,
        PlanId INT NOT NULL,
        SubscriptionId UNIQUEIDENTIFIER NULL,
        FamilyAdminUserId UNIQUEIDENTIFIER NOT NULL,
        FamilyScore INT NOT NULL CONSTRAINT DF_Families_FamilyScore DEFAULT 0,
        FamilyScoreUpdatedAt DATETIME2 NULL,
        CurrentStreakDays INT NOT NULL CONSTRAINT DF_Families_CurrentStreakDays DEFAULT 0,
        BestStreakDays INT NOT NULL CONSTRAINT DF_Families_BestStreakDays DEFAULT 0,
        TimezoneId NVARCHAR(100) NOT NULL CONSTRAINT DF_Families_TimezoneId DEFAULT N'Asia/Kolkata',
        IsActive BIT NOT NULL CONSTRAINT DF_Families_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Families_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Families_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Families_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_Families_Plans_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.Plans (PlanId),
        CONSTRAINT FK_Families_Users_FamilyAdminUserId FOREIGN KEY (FamilyAdminUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Families_JoinCode' AND object_id = OBJECT_ID(N'dbo.Families'))
BEGIN
    CREATE UNIQUE INDEX UX_Families_JoinCode ON dbo.Families (JoinCode) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Families_PlanId' AND object_id = OBJECT_ID(N'dbo.Families'))
BEGIN
    CREATE INDEX IX_Families_PlanId ON dbo.Families (PlanId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Families_FamilyAdminUserId' AND object_id = OBJECT_ID(N'dbo.Families'))
BEGIN
    CREATE INDEX IX_Families_FamilyAdminUserId ON dbo.Families (FamilyAdminUserId);
END;
GO
