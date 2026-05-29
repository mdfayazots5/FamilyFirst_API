IF OBJECT_ID(N'dbo.TeacherFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherFeedback
    (
        FeedbackId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TeacherFeedback PRIMARY KEY DEFAULT NEWID(),
        TeacherProfileId UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        SessionId UNIQUEIDENTIFIER NULL,
        FeedbackType INT NOT NULL,
        Severity INT NULL,
        Subject NVARCHAR(300) NULL,
        Message NVARCHAR(2000) NOT NULL,
        CommentTemplateId UNIQUEIDENTIFIER NULL,
        WeeklySummaryJson NVARCHAR(MAX) NULL,
        IsAcknowledged BIT NOT NULL CONSTRAINT DF_TeacherFeedback_IsAcknowledged DEFAULT 0,
        AcknowledgedAt DATETIME2 NULL,
        AcknowledgedByUserId UNIQUEIDENTIFIER NULL,
        ParentResponseText NVARCHAR(1000) NULL,
        ResolutionStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_TeacherFeedback_ResolutionStatus DEFAULT N'Open',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherFeedback_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherFeedback_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsEditable AS (CASE WHEN DATEDIFF(HOUR, CreatedAt, GETUTCDATE()) < 24 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END),
        IsDeleted BIT NOT NULL CONSTRAINT DF_TeacherFeedback_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_TeacherFeedback_TeacherProfiles_TeacherProfileId FOREIGN KEY (TeacherProfileId) REFERENCES dbo.TeacherProfiles (TeacherProfileId),
        CONSTRAINT FK_TeacherFeedback_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_TeacherFeedback_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_TeacherFeedback_AttendanceSessions_SessionId FOREIGN KEY (SessionId) REFERENCES dbo.AttendanceSessions (SessionId),
        CONSTRAINT FK_TeacherFeedback_CommentTemplates_CommentTemplateId FOREIGN KEY (CommentTemplateId) REFERENCES dbo.CommentTemplates (TemplateId),
        CONSTRAINT FK_TeacherFeedback_Users_AcknowledgedByUserId FOREIGN KEY (AcknowledgedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_TeacherFeedback_ResolutionStatus CHECK (ResolutionStatus IN (N'Open', N'Acknowledged', N'Resolved')),
        CONSTRAINT CK_TeacherFeedback_WeeklySummaryJson CHECK (WeeklySummaryJson IS NULL OR ISJSON(WeeklySummaryJson) = 1)
    );
END;
GO
