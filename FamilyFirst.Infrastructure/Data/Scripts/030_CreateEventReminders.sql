IF OBJECT_ID(N'dbo.tblEventReminder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEventReminder
    (
        EventReminderId         BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblEventReminder_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        CalendarEventId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        RemindBeforeMinutes     INT NOT NULL,
        Channel                 INT NOT NULL,
        IsSent                  BIT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_IsSent DEFAULT (0),
        SentAt                  DATETIME2 NULL,
        ScheduledFor            DATETIME2 NOT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblEventReminder_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblEventReminder_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblEventReminder_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblEventReminder_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblEventReminder_EventReminderId PRIMARY KEY (EventReminderId),
        CONSTRAINT FK_tblEventReminder_CalendarEventId_tblCalendarEvent_CalendarEventId
            FOREIGN KEY (CalendarEventId) REFERENCES dbo.tblCalendarEvent (CalendarEventId),
        CONSTRAINT FK_tblEventReminder_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblEventReminder_RemindBeforeMinutes
            CHECK (RemindBeforeMinutes IN (5, 10, 15, 30, 60, 120, 480, 1440, 4320))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblEventReminder_Id' AND object_id = OBJECT_ID(N'dbo.tblEventReminder'))
BEGIN
    CREATE UNIQUE INDEX UK_tblEventReminder_Id ON dbo.tblEventReminder (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblEventReminder_CalendarEventId' AND object_id = OBJECT_ID(N'dbo.tblEventReminder'))
BEGIN
    CREATE INDEX IDX_tblEventReminder_CalendarEventId
        ON dbo.tblEventReminder (CalendarEventId)
        WHERE IsDeleted = 0;
END;
GO
