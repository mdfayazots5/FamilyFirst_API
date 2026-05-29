IF OBJECT_ID(N'dbo.NotificationPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationPreferences
    (
        PreferenceId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationPreferences PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        AttendanceAlerts BIT NOT NULL CONSTRAINT DF_NotificationPreferences_AttendanceAlerts DEFAULT 1,
        FeedbackAlerts BIT NOT NULL CONSTRAINT DF_NotificationPreferences_FeedbackAlerts DEFAULT 1,
        TaskVerificationAlerts BIT NOT NULL CONSTRAINT DF_NotificationPreferences_TaskVerificationAlerts DEFAULT 1,
        RewardAlerts BIT NOT NULL CONSTRAINT DF_NotificationPreferences_RewardAlerts DEFAULT 1,
        CalendarAlerts BIT NOT NULL CONSTRAINT DF_NotificationPreferences_CalendarAlerts DEFAULT 1,
        WeeklyDigest BIT NOT NULL CONSTRAINT DF_NotificationPreferences_WeeklyDigest DEFAULT 1,
        QuietHoursEnabled BIT NOT NULL CONSTRAINT DF_NotificationPreferences_QuietHoursEnabled DEFAULT 1,
        QuietHoursStartTime TIME NOT NULL CONSTRAINT DF_NotificationPreferences_QuietHoursStartTime DEFAULT CAST('22:00:00' AS TIME),
        QuietHoursEndTime TIME NOT NULL CONSTRAINT DF_NotificationPreferences_QuietHoursEndTime DEFAULT CAST('07:00:00' AS TIME),
        MorningDigestTime TIME NOT NULL CONSTRAINT DF_NotificationPreferences_MorningDigestTime DEFAULT CAST('07:00:00' AS TIME),
        EveningDigestTime TIME NOT NULL CONSTRAINT DF_NotificationPreferences_EveningDigestTime DEFAULT CAST('20:00:00' AS TIME),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotificationPreferences_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_NotificationPreferences_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_NotificationPreferences_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UX_NotificationPreferences_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.NotificationPreferences')
)
BEGIN
    CREATE UNIQUE INDEX UX_NotificationPreferences_UserId
        ON dbo.NotificationPreferences (UserId);
END;
GO
