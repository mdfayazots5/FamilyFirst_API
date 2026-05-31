IF OBJECT_ID(N'dbo.tblChildProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblChildProfile
    (
        ChildProfileId              BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblChildProfile_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        FamilyMemberId              BIGINT NOT NULL,
        UserId                      BIGINT NOT NULL,
        FamilyId                    BIGINT NOT NULL,
        DateOfBirth                 DATETIME2 NULL,
        GradeLevel                  NVARCHAR(64) NULL,
        SchoolName                  NVARCHAR(256) NULL,
        AvatarCode                  NVARCHAR(24) NOT NULL
                                        CONSTRAINT DF_tblChildProfile_AvatarCode DEFAULT (N'avatar_01'),
        CoinBalance                 INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_CoinBalance DEFAULT (0),
        TotalCoinsEarned            INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_TotalCoinsEarned DEFAULT (0),
        CurrentStreakDays            INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_CurrentStreakDays DEFAULT (0),
        BestStreakDays              INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_BestStreakDays DEFAULT (0),
        StreakFreezesAvailable      INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_StreakFreezesAvailable DEFAULT (0),
        LevelCode                   INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_LevelCode DEFAULT (1),
        StudyScore                  INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_StudyScore DEFAULT (0),
        CleanlinessScore            INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_CleanlinessScore DEFAULT (0),
        DisciplineScore             INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_DisciplineScore DEFAULT (0),
        ScreenControlScore          INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_ScreenControlScore DEFAULT (0),
        ResponsibilityScore         INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_ResponsibilityScore DEFAULT (0),
        ScoreUpdatedAt              DATETIME2 NULL,

        -- Computed column: age in years based on DateOfBirth vs DateCreated
        AgeYears AS
        (
            CASE
                WHEN DateOfBirth IS NULL THEN NULL
                ELSE
                    DATEDIFF(YEAR, DateOfBirth, CONVERT(DATE, DateCreated))
                    - CASE
                        WHEN DATEADD(YEAR, DATEDIFF(YEAR, DateOfBirth, CONVERT(DATE, DateCreated)), DateOfBirth)
                             > CONVERT(DATE, DateCreated)
                            THEN 1
                        ELSE 0
                      END
            END
        ),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblChildProfile_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblChildProfile_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblChildProfile_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblChildProfile_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblChildProfile_ChildProfileId PRIMARY KEY (ChildProfileId),
        CONSTRAINT FK_tblChildProfile_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT FK_tblChildProfile_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblChildProfile_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblChildProfile_AvatarCode
            CHECK (AvatarCode IN (N'avatar_01', N'avatar_02', N'avatar_03', N'avatar_04', N'avatar_05',
                                  N'avatar_06', N'avatar_07', N'avatar_08', N'avatar_09', N'avatar_10')),
        CONSTRAINT CK_tblChildProfile_CoinBalance
            CHECK (CoinBalance >= 0),
        CONSTRAINT CK_tblChildProfile_TotalCoinsEarned
            CHECK (TotalCoinsEarned >= 0),
        CONSTRAINT CK_tblChildProfile_StreakFreezesAvailable
            CHECK (StreakFreezesAvailable BETWEEN 0 AND 2),
        CONSTRAINT CK_tblChildProfile_LevelCode
            CHECK (LevelCode >= 1),
        CONSTRAINT CK_tblChildProfile_StudyScore
            CHECK (StudyScore BETWEEN 0 AND 20),
        CONSTRAINT CK_tblChildProfile_CleanlinessScore
            CHECK (CleanlinessScore BETWEEN 0 AND 20),
        CONSTRAINT CK_tblChildProfile_DisciplineScore
            CHECK (DisciplineScore BETWEEN 0 AND 20),
        CONSTRAINT CK_tblChildProfile_ScreenControlScore
            CHECK (ScreenControlScore BETWEEN 0 AND 20),
        CONSTRAINT CK_tblChildProfile_ResponsibilityScore
            CHECK (ResponsibilityScore BETWEEN 0 AND 20)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblChildProfile_Id' AND object_id = OBJECT_ID(N'dbo.tblChildProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblChildProfile_Id ON dbo.tblChildProfile (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblChildProfile_FamilyMemberId' AND object_id = OBJECT_ID(N'dbo.tblChildProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblChildProfile_FamilyMemberId
        ON dbo.tblChildProfile (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblChildProfile_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblChildProfile'))
BEGIN
    CREATE INDEX IDX_tblChildProfile_FamilyId ON dbo.tblChildProfile (FamilyId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblChildProfile_UserId' AND object_id = OBJECT_ID(N'dbo.tblChildProfile'))
BEGIN
    CREATE INDEX IDX_tblChildProfile_UserId ON dbo.tblChildProfile (UserId);
END;
GO
