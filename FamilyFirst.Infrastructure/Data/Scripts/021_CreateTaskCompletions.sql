IF OBJECT_ID(N'dbo.TaskCompletions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskCompletions
    (
        CompletionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TaskCompletions PRIMARY KEY DEFAULT NEWID(),
        TaskId UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        ScheduledDate DATE NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_TaskCompletions_Status DEFAULT 1,
        PhotoUrl NVARCHAR(500) NULL,
        SubmittedAt DATETIME2 NULL,
        ReviewedByUserId UNIQUEIDENTIFIER NULL,
        ReviewedAt DATETIME2 NULL,
        ReviewNote NVARCHAR(500) NULL,
        CoinsAwarded INT NOT NULL CONSTRAINT DF_TaskCompletions_CoinsAwarded DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TaskCompletions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_TaskCompletions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_TaskCompletions_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_TaskCompletions_TaskItems_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.TaskItems (TaskId),
        CONSTRAINT FK_TaskCompletions_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_TaskCompletions_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_TaskCompletions_Users_ReviewedByUserId FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TaskCompletions_Task_Child_Date'
        AND idx.object_id = OBJECT_ID(N'dbo.TaskCompletions')
)
BEGIN
    CREATE UNIQUE INDEX IX_TaskCompletions_Task_Child_Date
        ON dbo.TaskCompletions
        (
            TaskId,
            ChildProfileId,
            ScheduledDate
        )
        WHERE IsDeleted = 0;
END;
GO
