IF OBJECT_ID(N'dbo.EventReminders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventReminders
    (
        ReminderId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EventReminders PRIMARY KEY DEFAULT NEWID(),
        EventId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        RemindBeforeMinutes INT NOT NULL,
        Channel INT NOT NULL,
        IsSent BIT NOT NULL CONSTRAINT DF_EventReminders_IsSent DEFAULT 0,
        SentAt DATETIME2 NULL,
        ScheduledFor DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EventReminders_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_EventReminders_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_EventReminders_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_EventReminders_CalendarEvents_EventId FOREIGN KEY (EventId) REFERENCES dbo.CalendarEvents (EventId),
        CONSTRAINT FK_EventReminders_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_EventReminders_RemindBeforeMinutes CHECK (RemindBeforeMinutes IN (5, 10, 15, 30, 60, 120, 480, 1440, 4320))
    );
END;
GO
