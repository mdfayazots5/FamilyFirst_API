IF OBJECT_ID(N'dbo.tblAttendanceSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAttendanceSession
    (
        AttendanceSessionId     BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        TeacherProfileId        BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        SessionName             NVARCHAR(256) NOT NULL,
        SubjectName             NVARCHAR(256) NOT NULL,
        BatchName               NVARCHAR(128) NULL,
        ScheduledDate           DATETIME2 NOT NULL,
        StartTime               DATETIME2 NOT NULL,
        EndTime                 DATETIME2 NULL,
        IsSubmitted             BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IsSubmitted DEFAULT (0),
        SubmittedAt             DATETIME2 NULL,
        IsRecurring             BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IsRecurring DEFAULT (0),
        RecurringDays           NVARCHAR(64) NULL,
        IsActive                BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IsActive DEFAULT (1),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceSession_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblAttendanceSession_AttendanceSessionId
            PRIMARY KEY (AttendanceSessionId),
        CONSTRAINT FK_tblAttendanceSession_TeacherProfileId_tblTeacherProfile_TeacherProfileId
            FOREIGN KEY (TeacherProfileId) REFERENCES dbo.tblTeacherProfile (TeacherProfileId),
        CONSTRAINT FK_tblAttendanceSession_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblAttendanceSession_TimeRange
            CHECK (EndTime IS NULL OR EndTime > StartTime),
        CONSTRAINT CK_tblAttendanceSession_RecurringDaysJson
            CHECK (RecurringDays IS NULL OR ISJSON(RecurringDays) = 1),
        CONSTRAINT CK_tblAttendanceSession_RecurringDaysRequired
            CHECK (IsRecurring = 0 OR RecurringDays IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblAttendanceSession_Id' AND object_id = OBJECT_ID(N'dbo.tblAttendanceSession'))
BEGIN
    CREATE UNIQUE INDEX UK_tblAttendanceSession_Id
        ON dbo.tblAttendanceSession (Id) WHERE IsDeleted = 0;
END;
GO
