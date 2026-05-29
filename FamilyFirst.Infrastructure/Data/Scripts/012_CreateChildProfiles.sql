IF OBJECT_ID(N'dbo.ChildProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChildProfiles
    (
        ChildProfileId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChildProfiles PRIMARY KEY DEFAULT NEWID(),
        FamilyMemberId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        DateOfBirth DATE NULL,
        GradeLevel NVARCHAR(50) NULL,
        SchoolName NVARCHAR(200) NULL,
        AvatarCode NVARCHAR(20) NOT NULL CONSTRAINT DF_ChildProfiles_AvatarCode DEFAULT N'avatar_01',
        CoinBalance INT NOT NULL CONSTRAINT DF_ChildProfiles_CoinBalance DEFAULT 0,
        TotalCoinsEarned INT NOT NULL CONSTRAINT DF_ChildProfiles_TotalCoinsEarned DEFAULT 0,
        CurrentStreakDays INT NOT NULL CONSTRAINT DF_ChildProfiles_CurrentStreakDays DEFAULT 0,
        BestStreakDays INT NOT NULL CONSTRAINT DF_ChildProfiles_BestStreakDays DEFAULT 0,
        StreakFreezesAvailable INT NOT NULL CONSTRAINT DF_ChildProfiles_StreakFreezesAvailable DEFAULT 0,
        LevelCode INT NOT NULL CONSTRAINT DF_ChildProfiles_LevelCode DEFAULT 1,
        StudyScore INT NOT NULL CONSTRAINT DF_ChildProfiles_StudyScore DEFAULT 0,
        CleanlinessScore INT NOT NULL CONSTRAINT DF_ChildProfiles_CleanlinessScore DEFAULT 0,
        DisciplineScore INT NOT NULL CONSTRAINT DF_ChildProfiles_DisciplineScore DEFAULT 0,
        ScreenControlScore INT NOT NULL CONSTRAINT DF_ChildProfiles_ScreenControlScore DEFAULT 0,
        ResponsibilityScore INT NOT NULL CONSTRAINT DF_ChildProfiles_ResponsibilityScore DEFAULT 0,
        ScoreUpdatedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChildProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChildProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_ChildProfiles_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        AgeYears AS
        (
            CASE
                WHEN DateOfBirth IS NULL THEN NULL
                ELSE
                    DATEDIFF(YEAR, DateOfBirth, CONVERT(DATE, CreatedAt))
                    - CASE
                        WHEN DATEADD(YEAR, DATEDIFF(YEAR, DateOfBirth, CONVERT(DATE, CreatedAt)), DateOfBirth) > CONVERT(DATE, CreatedAt)
                            THEN 1
                        ELSE 0
                    END
            END
        ),
        CONSTRAINT FK_ChildProfiles_FamilyMembers_FamilyMemberId FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT FK_ChildProfiles_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_ChildProfiles_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_ChildProfiles_AvatarCode CHECK (AvatarCode IN (N'avatar_01', N'avatar_02', N'avatar_03', N'avatar_04', N'avatar_05', N'avatar_06', N'avatar_07', N'avatar_08', N'avatar_09', N'avatar_10')),
        CONSTRAINT CK_ChildProfiles_CoinBalance CHECK (CoinBalance >= 0),
        CONSTRAINT CK_ChildProfiles_TotalCoinsEarned CHECK (TotalCoinsEarned >= 0),
        CONSTRAINT CK_ChildProfiles_StreakFreezesAvailable CHECK (StreakFreezesAvailable BETWEEN 0 AND 2),
        CONSTRAINT CK_ChildProfiles_LevelCode CHECK (LevelCode >= 1),
        CONSTRAINT CK_ChildProfiles_StudyScore CHECK (StudyScore BETWEEN 0 AND 20),
        CONSTRAINT CK_ChildProfiles_CleanlinessScore CHECK (CleanlinessScore BETWEEN 0 AND 20),
        CONSTRAINT CK_ChildProfiles_DisciplineScore CHECK (DisciplineScore BETWEEN 0 AND 20),
        CONSTRAINT CK_ChildProfiles_ScreenControlScore CHECK (ScreenControlScore BETWEEN 0 AND 20),
        CONSTRAINT CK_ChildProfiles_ResponsibilityScore CHECK (ResponsibilityScore BETWEEN 0 AND 20)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UX_ChildProfiles_FamilyMemberId'
        AND idx.object_id = OBJECT_ID(N'dbo.ChildProfiles')
)
BEGIN
    CREATE UNIQUE INDEX UX_ChildProfiles_FamilyMemberId
        ON dbo.ChildProfiles
        (
            FamilyMemberId
        )
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_ChildProfiles_FamilyId'
        AND idx.object_id = OBJECT_ID(N'dbo.ChildProfiles')
)
BEGIN
    CREATE INDEX IX_ChildProfiles_FamilyId
        ON dbo.ChildProfiles
        (
            FamilyId
        );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_ChildProfiles_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.ChildProfiles')
)
BEGIN
    CREATE INDEX IX_ChildProfiles_UserId
        ON dbo.ChildProfiles
        (
            UserId
        );
END;
GO
