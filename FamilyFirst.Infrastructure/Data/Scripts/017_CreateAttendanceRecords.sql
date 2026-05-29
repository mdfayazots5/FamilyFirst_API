IF OBJECT_ID(N'dbo.AttendanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttendanceRecords
    (
        RecordId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY DEFAULT NEWID(),
        SessionId UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        Status INT NOT NULL,
        TeacherComment NVARCHAR(500) NULL,
        CommentTemplateId UNIQUEIDENTIFIER NULL,
        MarkedAt DATETIME2 NOT NULL CONSTRAINT DF_AttendanceRecords_MarkedAt DEFAULT SYSUTCDATETIME(),
        MarkedByUserId UNIQUEIDENTIFIER NOT NULL,
        EditedAt DATETIME2 NULL,
        EditedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AttendanceRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_AttendanceRecords_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_AttendanceRecords_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_AttendanceRecords_AttendanceSessions_SessionId FOREIGN KEY (SessionId) REFERENCES dbo.AttendanceSessions (SessionId),
        CONSTRAINT FK_AttendanceRecords_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_AttendanceRecords_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_AttendanceRecords_CommentTemplates_CommentTemplateId FOREIGN KEY (CommentTemplateId) REFERENCES dbo.CommentTemplates (TemplateId),
        CONSTRAINT FK_AttendanceRecords_Users_MarkedByUserId FOREIGN KEY (MarkedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_AttendanceRecords_Users_EditedByUserId FOREIGN KEY (EditedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_AttendanceRecords_Status CHECK (Status IN (1, 2, 3, 4))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AttendanceRecords_Session_Child'
        AND idx.object_id = OBJECT_ID(N'dbo.AttendanceRecords')
)
BEGIN
    CREATE UNIQUE INDEX IX_AttendanceRecords_Session_Child
        ON dbo.AttendanceRecords
        (
            SessionId,
            ChildProfileId
        )
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AttendanceRecords_FamilyId_ChildProfileId'
        AND idx.object_id = OBJECT_ID(N'dbo.AttendanceRecords')
)
BEGIN
    CREATE INDEX IX_AttendanceRecords_FamilyId_ChildProfileId
        ON dbo.AttendanceRecords
        (
            FamilyId,
            ChildProfileId
        )
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AttendanceRecords_SessionId'
        AND idx.object_id = OBJECT_ID(N'dbo.AttendanceRecords')
)
BEGIN
    CREATE INDEX IX_AttendanceRecords_SessionId
        ON dbo.AttendanceRecords
        (
            SessionId
        )
        WHERE IsDeleted = 0;
END;
GO
