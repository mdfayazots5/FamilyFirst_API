IF OBJECT_ID(N'dbo.TaskItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskItems
    (
        TaskId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TaskItems PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NULL,
        ChildProfileId UNIQUEIDENTIFIER NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        TaskName NVARCHAR(200) NOT NULL,
        Instructions NVARCHAR(1000) NULL,
        IconCode NVARCHAR(50) NULL,
        TimeBlock INT NOT NULL,
        DurationMinutes INT NOT NULL CONSTRAINT DF_TaskItems_DurationMinutes DEFAULT 15,
        CoinValue INT NOT NULL CONSTRAINT DF_TaskItems_CoinValue DEFAULT 10,
        IsPhotoRequired BIT NOT NULL CONSTRAINT DF_TaskItems_IsPhotoRequired DEFAULT 0,
        PillarTag NVARCHAR(50) NULL,
        IsRecurring BIT NOT NULL CONSTRAINT DF_TaskItems_IsRecurring DEFAULT 1,
        RecurringDays NVARCHAR(50) NOT NULL CONSTRAINT DF_TaskItems_RecurringDays DEFAULT N'[1,2,3,4,5,6,7]',
        ActiveFromDate DATE NOT NULL CONSTRAINT DF_TaskItems_ActiveFromDate DEFAULT CAST(SYSUTCDATETIME() AS DATE),
        ActiveToDate DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TaskItems_IsActive DEFAULT 1,
        IsSystemTemplate BIT NOT NULL CONSTRAINT DF_TaskItems_IsSystemTemplate DEFAULT 0,
        TemplateCategory NVARCHAR(50) NULL,
        AgeGroup NVARCHAR(50) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TaskItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_TaskItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_TaskItems_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_TaskItems_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_TaskItems_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_TaskItems_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_TaskItems_RecurringDaysJson CHECK (ISJSON(RecurringDays) = 1),
        CONSTRAINT CK_TaskItems_ActiveDateRange CHECK (ActiveToDate IS NULL OR ActiveToDate > ActiveFromDate),
        CONSTRAINT CK_TaskItems_PillarTag CHECK (PillarTag IS NULL OR PillarTag IN (N'Study', N'Cleanliness', N'Discipline', N'ScreenControl', N'Responsibility')),
        CONSTRAINT CK_TaskItems_TemplateShape CHECK
        (
            (IsSystemTemplate = 0 AND FamilyId IS NOT NULL)
            OR
            (IsSystemTemplate = 1 AND FamilyId IS NULL AND ChildProfileId IS NULL AND TemplateCategory IS NOT NULL)
        )
    );
END;
GO
