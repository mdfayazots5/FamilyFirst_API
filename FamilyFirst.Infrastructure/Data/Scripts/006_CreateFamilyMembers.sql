IF OBJECT_ID(N'dbo.FamilyMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyMembers
    (
        FamilyMemberId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FamilyMembers PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Role INT NOT NULL,
        LinkType NVARCHAR(50) NOT NULL,
        DisplayName NVARCHAR(200) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_FamilyMembers_IsActive DEFAULT 1,
        JoinedAt DATETIME2 NOT NULL CONSTRAINT DF_FamilyMembers_JoinedAt DEFAULT SYSUTCDATETIME(),
        InvitedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_FamilyMembers_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_FamilyMembers_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_FamilyMembers_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_FamilyMembers_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_FamilyMembers_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_FamilyMembers_Users_InvitedByUserId FOREIGN KEY (InvitedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_FamilyMembers_Role CHECK (Role IN (1, 2, 3, 4, 5, 6))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyMembers_FamilyId_UserId' AND object_id = OBJECT_ID(N'dbo.FamilyMembers'))
BEGIN
    CREATE UNIQUE INDEX IX_FamilyMembers_FamilyId_UserId
        ON dbo.FamilyMembers (FamilyId, UserId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyMembers_UserId' AND object_id = OBJECT_ID(N'dbo.FamilyMembers'))
BEGIN
    CREATE INDEX IX_FamilyMembers_UserId ON dbo.FamilyMembers (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyMembers_InvitedByUserId' AND object_id = OBJECT_ID(N'dbo.FamilyMembers'))
BEGIN
    CREATE INDEX IX_FamilyMembers_InvitedByUserId ON dbo.FamilyMembers (InvitedByUserId);
END;
GO
