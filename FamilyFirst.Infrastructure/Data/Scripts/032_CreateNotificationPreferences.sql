-- TIME columns are stored as DATETIME2 per SQL Format standard.
-- Defaults anchor to 1900-01-01; only the time portion is meaningful at the application layer.
IF OBJECT_ID(N'dbo.tblNotificationPreference', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblNotificationPreference
    (
        NotificationPreferenceId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        UserId                      BIGINT NOT NULL,
        FamilyId                    BIGINT NOT NULL,
        AttendanceAlerts            BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_AttendanceAlerts DEFAULT (1),
        FeedbackAlerts              BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_FeedbackAlerts DEFAULT (1),
        TaskVerificationAlerts      BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_TaskVerificationAlerts DEFAULT (1),
        RewardAlerts                BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_RewardAlerts DEFAULT (1),
        CalendarAlerts              BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_CalendarAlerts DEFAULT (1),
        WeeklyDigest                BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_WeeklyDigest DEFAULT (1),
        QuietHoursEnabled           BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_QuietHoursEnabled DEFAULT (1),
        QuietHoursStartTime         DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_QuietHoursStartTime
                                            DEFAULT (CAST('1900-01-01 22:00:00' AS DATETIME2)),
        QuietHoursEndTime           DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_QuietHoursEndTime
                                            DEFAULT (CAST('1900-01-01 07:00:00' AS DATETIME2)),
        MorningDigestTime           DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_MorningDigestTime
                                            DEFAULT (CAST('1900-01-01 07:00:00' AS DATETIME2)),
        EveningDigestTime           DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_EveningDigestTime
                                            DEFAULT (CAST('1900-01-01 20:00:00' AS DATETIME2)),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblNotificationPreference_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblNotificationPreference_NotificationPreferenceId
            PRIMARY KEY (NotificationPreferenceId),
        CONSTRAINT FK_tblNotificationPreference_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblNotificationPreference_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblNotificationPreference_Id' AND object_id = OBJECT_ID(N'dbo.tblNotificationPreference'))
BEGIN
    CREATE UNIQUE INDEX UK_tblNotificationPreference_Id
        ON dbo.tblNotificationPreference (Id) WHERE IsDeleted = 0;
END;
GO

-- One preference record per user (user-level setting, not per-family)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblNotificationPreference_UserId' AND object_id = OBJECT_ID(N'dbo.tblNotificationPreference'))
BEGIN
    CREATE UNIQUE INDEX UK_tblNotificationPreference_UserId
        ON dbo.tblNotificationPreference (UserId);
END;
GO
