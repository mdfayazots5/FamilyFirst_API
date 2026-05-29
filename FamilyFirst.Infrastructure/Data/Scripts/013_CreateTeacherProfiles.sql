IF OBJECT_ID(N'dbo.TeacherProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherProfiles
    (
        TeacherProfileId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TeacherProfiles PRIMARY KEY DEFAULT NEWID(),
        FamilyMemberId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        SubjectName NVARCHAR(200) NOT NULL,
        TeacherType NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TeacherProfiles_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_TeacherProfiles_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_TeacherProfiles_FamilyMembers_FamilyMemberId FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT FK_TeacherProfiles_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_TeacherProfiles_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_TeacherProfiles_TeacherType CHECK (TeacherType IN (N'School', N'Tuition', N'Arabic', N'Music', N'Other'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UX_TeacherProfiles_FamilyMemberId'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherProfiles')
)
BEGIN
    CREATE UNIQUE INDEX UX_TeacherProfiles_FamilyMemberId
        ON dbo.TeacherProfiles
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
    WHERE idx.name = N'IX_TeacherProfiles_FamilyId'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherProfiles')
)
BEGIN
    CREATE INDEX IX_TeacherProfiles_FamilyId
        ON dbo.TeacherProfiles
        (
            FamilyId
        );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TeacherProfiles_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherProfiles')
)
BEGIN
    CREATE INDEX IX_TeacherProfiles_UserId
        ON dbo.TeacherProfiles
        (
            UserId
        );
END;
GO
