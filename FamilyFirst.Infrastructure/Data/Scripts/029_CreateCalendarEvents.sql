IF OBJECT_ID(N'dbo.CalendarEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CalendarEvents
    (
        EventId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CalendarEvents PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        EventTitle NVARCHAR(300) NOT NULL,
        EventType INT NOT NULL,
        Description NVARCHAR(1000) NULL,
        StartDateTime DATETIME2 NOT NULL,
        EndDateTime DATETIME2 NULL,
        IsAllDay BIT NOT NULL CONSTRAINT DF_CalendarEvents_IsAllDay DEFAULT 0,
        Location NVARCHAR(300) NULL,
        ColorHex NVARCHAR(7) NULL,
        IsRecurring BIT NOT NULL CONSTRAINT DF_CalendarEvents_IsRecurring DEFAULT 0,
        RecurrenceRule NVARCHAR(200) NULL,
        VisibilityScope NVARCHAR(50) NOT NULL CONSTRAINT DF_CalendarEvents_VisibilityScope DEFAULT N'Family',
        LinkedChildProfileId UNIQUEIDENTIFIER NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CalendarEvents_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CalendarEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CalendarEvents_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_CalendarEvents_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_CalendarEvents_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_CalendarEvents_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_CalendarEvents_ChildProfiles_LinkedChildProfileId FOREIGN KEY (LinkedChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT CK_CalendarEvents_VisibilityScope CHECK (VisibilityScope IN (N'Family', N'Parent', N'Child', N'Elder', N'Caregiver')),
        CONSTRAINT CK_CalendarEvents_EndDateTime CHECK (EndDateTime IS NULL OR EndDateTime >= StartDateTime)
    );
END;
GO
