IF OBJECT_ID(N'dbo.CommentTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommentTemplates
    (
        TemplateId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CommentTemplates PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NULL,
        TemplateText NVARCHAR(500) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        IsSystem BIT NOT NULL CONSTRAINT DF_CommentTemplates_IsSystem DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_CommentTemplates_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_CommentTemplates_SortOrder DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CommentTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CommentTemplates_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_CommentTemplates_Category CHECK (Category IN (N'Attendance', N'Feedback', N'Homework'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommentTemplates_FamilyId_Category' AND object_id = OBJECT_ID(N'dbo.CommentTemplates'))
BEGIN
    CREATE INDEX IX_CommentTemplates_FamilyId_Category ON dbo.CommentTemplates (FamilyId, Category, IsActive);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CommentTemplates WHERE IsSystem = 1 AND Category = N'Attendance' AND TemplateText = N'Great punctuality today.')
BEGIN
    INSERT INTO dbo.CommentTemplates (TemplateText, Category, IsSystem, IsActive, SortOrder)
    VALUES
        (N'Great punctuality today.', N'Attendance', 1, 1, 10),
        (N'Arrived late and needs support with timing.', N'Attendance', 1, 1, 20),
        (N'Absent today. Please follow up at home.', N'Attendance', 1, 1, 30),
        (N'Left early with parent awareness.', N'Attendance', 1, 1, 40);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CommentTemplates WHERE IsSystem = 1 AND Category = N'Feedback' AND TemplateText = N'Excellent focus and participation.')
BEGIN
    INSERT INTO dbo.CommentTemplates (TemplateText, Category, IsSystem, IsActive, SortOrder)
    VALUES
        (N'Excellent focus and participation.', N'Feedback', 1, 1, 10),
        (N'Showed kindness and responsibility today.', N'Feedback', 1, 1, 20),
        (N'Needs gentle reminders to stay on task.', N'Feedback', 1, 1, 30),
        (N'Please discuss today''s concern at home.', N'Feedback', 1, 1, 40);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CommentTemplates WHERE IsSystem = 1 AND Category = N'Homework' AND TemplateText = N'Homework completed on time.')
BEGIN
    INSERT INTO dbo.CommentTemplates (TemplateText, Category, IsSystem, IsActive, SortOrder)
    VALUES
        (N'Homework completed on time.', N'Homework', 1, 1, 10),
        (N'Homework needs correction and resubmission.', N'Homework', 1, 1, 20),
        (N'Homework was incomplete today.', N'Homework', 1, 1, 30),
        (N'Please support homework practice at home.', N'Homework', 1, 1, 40);
END;
GO
