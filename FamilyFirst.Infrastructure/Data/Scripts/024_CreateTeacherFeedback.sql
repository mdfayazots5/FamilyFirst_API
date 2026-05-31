IF OBJECT_ID(N'dbo.tblTeacherFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTeacherFeedback
    (
        TeacherFeedbackId       BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        TeacherProfileId        BIGINT NOT NULL,
        ChildProfileId          BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        AttendanceSessionId     BIGINT NULL,
        FeedbackType            INT NOT NULL,
        Severity                INT NULL,
        Subject                 NVARCHAR(512) NULL,
        Message                 NVARCHAR(2048) NOT NULL,
        CommentTemplateId       BIGINT NULL,
        WeeklySummaryJson       NVARCHAR(MAX) NULL,
        IsAcknowledged          BIT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_IsAcknowledged DEFAULT (0),
        AcknowledgedAt          DATETIME2 NULL,
        AcknowledgedByUserId    BIGINT NULL,
        ParentResponseText      NVARCHAR(1024) NULL,
        ResolutionStatus        NVARCHAR(24) NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_ResolutionStatus DEFAULT (N'Open'),

        -- Computed column: editable within 24 hours of creation
        IsEditable AS
        (
            CASE
                WHEN DATEDIFF(HOUR, DateCreated, GETDATE()) < 24
                    THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END
        ),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblTeacherFeedback_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTeacherFeedback_TeacherFeedbackId PRIMARY KEY (TeacherFeedbackId),
        CONSTRAINT FK_tblTeacherFeedback_TeacherProfileId_tblTeacherProfile_TeacherProfileId
            FOREIGN KEY (TeacherProfileId) REFERENCES dbo.tblTeacherProfile (TeacherProfileId),
        CONSTRAINT FK_tblTeacherFeedback_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblTeacherFeedback_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblTeacherFeedback_AttendanceSessionId_tblAttendanceSession_AttendanceSessionId
            FOREIGN KEY (AttendanceSessionId) REFERENCES dbo.tblAttendanceSession (AttendanceSessionId),
        CONSTRAINT FK_tblTeacherFeedback_CommentTemplateId_tblCommentTemplate_CommentTemplateId
            FOREIGN KEY (CommentTemplateId) REFERENCES dbo.tblCommentTemplate (CommentTemplateId),
        CONSTRAINT FK_tblTeacherFeedback_AcknowledgedByUserId_tblUser_UserId
            FOREIGN KEY (AcknowledgedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblTeacherFeedback_ResolutionStatus
            CHECK (ResolutionStatus IN (N'Open', N'Acknowledged', N'Resolved')),
        CONSTRAINT CK_tblTeacherFeedback_WeeklySummaryJson
            CHECK (WeeklySummaryJson IS NULL OR ISJSON(WeeklySummaryJson) = 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTeacherFeedback_Id' AND object_id = OBJECT_ID(N'dbo.tblTeacherFeedback'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTeacherFeedback_Id ON dbo.tblTeacherFeedback (Id) WHERE IsDeleted = 0;
END;
GO
