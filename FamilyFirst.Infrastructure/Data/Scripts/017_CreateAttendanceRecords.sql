IF OBJECT_ID(N'dbo.tblAttendanceRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAttendanceRecord
    (
        AttendanceRecordId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        AttendanceSessionId     BIGINT NOT NULL,
        ChildProfileId          BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        Status                  INT NOT NULL,
        TeacherComment          NVARCHAR(512) NULL,
        CommentTemplateId       BIGINT NULL,
        MarkedAt                DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_MarkedAt DEFAULT (GETDATE()),
        MarkedByUserId          BIGINT NOT NULL,
        EditedAt                DATETIME2 NULL,
        EditedByUserId          BIGINT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblAttendanceRecord_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblAttendanceRecord_AttendanceRecordId
            PRIMARY KEY (AttendanceRecordId),
        CONSTRAINT FK_tblAttendanceRecord_AttendanceSessionId_tblAttendanceSession_AttendanceSessionId
            FOREIGN KEY (AttendanceSessionId) REFERENCES dbo.tblAttendanceSession (AttendanceSessionId),
        CONSTRAINT FK_tblAttendanceRecord_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblAttendanceRecord_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblAttendanceRecord_CommentTemplateId_tblCommentTemplate_CommentTemplateId
            FOREIGN KEY (CommentTemplateId) REFERENCES dbo.tblCommentTemplate (CommentTemplateId),
        CONSTRAINT FK_tblAttendanceRecord_MarkedByUserId_tblUser_UserId
            FOREIGN KEY (MarkedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblAttendanceRecord_EditedByUserId_tblUser_UserId
            FOREIGN KEY (EditedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblAttendanceRecord_Status
            CHECK (Status IN (1, 2, 3, 4))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblAttendanceRecord_Id' AND object_id = OBJECT_ID(N'dbo.tblAttendanceRecord'))
BEGIN
    CREATE UNIQUE INDEX UK_tblAttendanceRecord_Id
        ON dbo.tblAttendanceRecord (Id) WHERE IsDeleted = 0;
END;
GO

-- One attendance record per child per session
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblAttendanceRecord_AttendanceSessionId_ChildProfileId' AND object_id = OBJECT_ID(N'dbo.tblAttendanceRecord'))
BEGIN
    CREATE UNIQUE INDEX UK_tblAttendanceRecord_AttendanceSessionId_ChildProfileId
        ON dbo.tblAttendanceRecord (AttendanceSessionId, ChildProfileId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblAttendanceRecord_FamilyId_ChildProfileId' AND object_id = OBJECT_ID(N'dbo.tblAttendanceRecord'))
BEGIN
    CREATE INDEX IDX_tblAttendanceRecord_FamilyId_ChildProfileId
        ON dbo.tblAttendanceRecord (FamilyId, ChildProfileId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblAttendanceRecord_AttendanceSessionId' AND object_id = OBJECT_ID(N'dbo.tblAttendanceRecord'))
BEGIN
    CREATE INDEX IDX_tblAttendanceRecord_AttendanceSessionId
        ON dbo.tblAttendanceRecord (AttendanceSessionId)
        WHERE IsDeleted = 0;
END;
GO
