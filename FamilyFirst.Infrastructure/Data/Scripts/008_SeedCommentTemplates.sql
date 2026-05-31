IF OBJECT_ID(N'dbo.tblCommentTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCommentTemplate
    (
        CommentTemplateId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NULL,
        TemplateText        NVARCHAR(512) NOT NULL,
        Category            NVARCHAR(64) NOT NULL,
        IsSystem            BIT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_IsSystem DEFAULT (0),
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_IsActive DEFAULT (1),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblCommentTemplate_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblCommentTemplate_CommentTemplateId PRIMARY KEY (CommentTemplateId),
        CONSTRAINT FK_tblCommentTemplate_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblCommentTemplate_Category
            CHECK (Category IN (N'Attendance', N'Feedback', N'Homework'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCommentTemplate_Id' AND object_id = OBJECT_ID(N'dbo.tblCommentTemplate'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCommentTemplate_Id ON dbo.tblCommentTemplate (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblCommentTemplate_FamilyId_Category' AND object_id = OBJECT_ID(N'dbo.tblCommentTemplate'))
BEGIN
    CREATE INDEX IDX_tblCommentTemplate_FamilyId_Category
        ON dbo.tblCommentTemplate (FamilyId, Category, IsActive);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblCommentTemplate WHERE IsSystem = 1 AND Category = N'Attendance' AND TemplateText = N'Great punctuality today.')
BEGIN
    INSERT INTO dbo.tblCommentTemplate (TemplateText, Category, IsSystem, IsActive, SortOrder, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES
        (N'Great punctuality today.',                   N'Attendance', 1, 1, 10, 1, 1, N'System', N'127.0.0.1'),
        (N'Arrived late and needs support with timing.',N'Attendance', 1, 1, 20, 1, 1, N'System', N'127.0.0.1'),
        (N'Absent today. Please follow up at home.',    N'Attendance', 1, 1, 30, 1, 1, N'System', N'127.0.0.1'),
        (N'Left early with parent awareness.',          N'Attendance', 1, 1, 40, 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblCommentTemplate WHERE IsSystem = 1 AND Category = N'Feedback' AND TemplateText = N'Excellent focus and participation.')
BEGIN
    INSERT INTO dbo.tblCommentTemplate (TemplateText, Category, IsSystem, IsActive, SortOrder, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES
        (N'Excellent focus and participation.',             N'Feedback', 1, 1, 10, 1, 1, N'System', N'127.0.0.1'),
        (N'Showed kindness and responsibility today.',      N'Feedback', 1, 1, 20, 1, 1, N'System', N'127.0.0.1'),
        (N'Needs gentle reminders to stay on task.',        N'Feedback', 1, 1, 30, 1, 1, N'System', N'127.0.0.1'),
        (N'Please discuss today''s concern at home.',       N'Feedback', 1, 1, 40, 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblCommentTemplate WHERE IsSystem = 1 AND Category = N'Homework' AND TemplateText = N'Homework completed on time.')
BEGIN
    INSERT INTO dbo.tblCommentTemplate (TemplateText, Category, IsSystem, IsActive, SortOrder, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES
        (N'Homework completed on time.',                        N'Homework', 1, 1, 10, 1, 1, N'System', N'127.0.0.1'),
        (N'Homework needs correction and resubmission.',        N'Homework', 1, 1, 20, 1, 1, N'System', N'127.0.0.1'),
        (N'Homework was incomplete today.',                     N'Homework', 1, 1, 30, 1, 1, N'System', N'127.0.0.1'),
        (N'Please support homework practice at home.',          N'Homework', 1, 1, 40, 1, 1, N'System', N'127.0.0.1');
END;
GO
