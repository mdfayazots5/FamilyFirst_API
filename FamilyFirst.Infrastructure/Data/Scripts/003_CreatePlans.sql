IF OBJECT_ID(N'dbo.Plans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Plans
    (
        PlanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Plans PRIMARY KEY,
        PlanName NVARCHAR(100) NOT NULL,
        PlanCode NVARCHAR(50) NOT NULL,
        PriceMonthly DECIMAL(10,2) NOT NULL,
        MaxChildren INT NOT NULL,
        MaxTeachers INT NOT NULL,
        HasElderMode BIT NOT NULL CONSTRAINT DF_Plans_HasElderMode DEFAULT 0,
        HasWeeklyDigest BIT NOT NULL CONSTRAINT DF_Plans_HasWeeklyDigest DEFAULT 0,
        HasAdvancedReports BIT NOT NULL CONSTRAINT DF_Plans_HasAdvancedReports DEFAULT 0,
        StorageQuotaMb INT NOT NULL CONSTRAINT DF_Plans_StorageQuotaMb DEFAULT 0,
        TrialDays INT NOT NULL CONSTRAINT DF_Plans_TrialDays DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_Plans_IsActive DEFAULT 1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Plans_PlanName' AND object_id = OBJECT_ID(N'dbo.Plans'))
BEGIN
    CREATE UNIQUE INDEX UX_Plans_PlanName ON dbo.Plans (PlanName);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Plans_PlanCode' AND object_id = OBJECT_ID(N'dbo.Plans'))
BEGIN
    CREATE UNIQUE INDEX UX_Plans_PlanCode ON dbo.Plans (PlanCode);
END;
GO
