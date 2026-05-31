IF OBJECT_ID(N'dbo.tblCalendarEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCalendarEvent
    (
        CalendarEventId         BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        CreatedByUserId         BIGINT NOT NULL,
        EventTitle              NVARCHAR(512) NOT NULL,
        EventType               INT NOT NULL,
        Description             NVARCHAR(1024) NULL,
        StartDateTime           DATETIME2 NOT NULL,
        EndDateTime             DATETIME2 NULL,
        IsAllDay                BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IsAllDay DEFAULT (0),
        Location                NVARCHAR(512) NULL,
        ColorHex                NVARCHAR(8) NULL,
        IsRecurring             BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IsRecurring DEFAULT (0),
        RecurrenceRule          NVARCHAR(256) NULL,
        VisibilityScope         NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_VisibilityScope DEFAULT (N'Family'),
        LinkedChildProfileId    BIGINT NULL,
        IsActive                BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IsActive DEFAULT (1),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblCalendarEvent_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblCalendarEvent_CalendarEventId PRIMARY KEY (CalendarEventId),
        CONSTRAINT FK_tblCalendarEvent_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblCalendarEvent_CreatedByUserId_tblUser_UserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblCalendarEvent_LinkedChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (LinkedChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT CK_tblCalendarEvent_VisibilityScope
            CHECK (VisibilityScope IN (N'Family', N'Parent', N'Child', N'Elder', N'Caregiver')),
        CONSTRAINT CK_tblCalendarEvent_EndDateTime
            CHECK (EndDateTime IS NULL OR EndDateTime >= StartDateTime)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCalendarEvent_Id' AND object_id = OBJECT_ID(N'dbo.tblCalendarEvent'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCalendarEvent_Id ON dbo.tblCalendarEvent (Id) WHERE IsDeleted = 0;
END;
GO
