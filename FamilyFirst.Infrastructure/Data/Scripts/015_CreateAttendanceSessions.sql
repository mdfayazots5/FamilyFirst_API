IF OBJECT_ID(N'dbo.AttendanceSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttendanceSessions
    (
        SessionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AttendanceSessions PRIMARY KEY DEFAULT NEWID(),
        TeacherProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        SessionName NVARCHAR(200) NOT NULL,
        SubjectName NVARCHAR(200) NOT NULL,
        BatchName NVARCHAR(100) NULL,
        ScheduledDate DATE NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NULL,
        IsSubmitted BIT NOT NULL CONSTRAINT DF_AttendanceSessions_IsSubmitted DEFAULT 0,
        SubmittedAt DATETIME2 NULL,
        IsRecurring BIT NOT NULL CONSTRAINT DF_AttendanceSessions_IsRecurring DEFAULT 0,
        RecurringDays NVARCHAR(50) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AttendanceSessions_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AttendanceSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_AttendanceSessions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_AttendanceSessions_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_AttendanceSessions_TeacherProfiles_TeacherProfileId FOREIGN KEY (TeacherProfileId) REFERENCES dbo.TeacherProfiles (TeacherProfileId),
        CONSTRAINT FK_AttendanceSessions_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_AttendanceSessions_TimeRange CHECK (EndTime IS NULL OR EndTime > StartTime),
        CONSTRAINT CK_AttendanceSessions_RecurringDaysJson CHECK (RecurringDays IS NULL OR ISJSON(RecurringDays) = 1),
        CONSTRAINT CK_AttendanceSessions_RecurringDaysRequired CHECK (IsRecurring = 0 OR RecurringDays IS NOT NULL)
    );
END;
GO
