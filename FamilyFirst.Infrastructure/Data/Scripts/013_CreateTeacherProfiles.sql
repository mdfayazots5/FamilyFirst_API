IF OBJECT_ID(N'dbo.tblTeacherProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTeacherProfile
    (
        TeacherProfileId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyMemberId      BIGINT NOT NULL,
        UserId              BIGINT NOT NULL,
        FamilyId            BIGINT NOT NULL,
        SubjectName         NVARCHAR(256) NOT NULL,
        TeacherType         NVARCHAR(64) NOT NULL,
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_IsActive DEFAULT (1),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblTeacherProfile_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTeacherProfile_TeacherProfileId PRIMARY KEY (TeacherProfileId),
        CONSTRAINT FK_tblTeacherProfile_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT FK_tblTeacherProfile_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblTeacherProfile_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblTeacherProfile_TeacherType
            CHECK (TeacherType IN (N'School', N'Tuition', N'Arabic', N'Music', N'Other'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTeacherProfile_Id' AND object_id = OBJECT_ID(N'dbo.tblTeacherProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTeacherProfile_Id ON dbo.tblTeacherProfile (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTeacherProfile_FamilyMemberId' AND object_id = OBJECT_ID(N'dbo.tblTeacherProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTeacherProfile_FamilyMemberId
        ON dbo.tblTeacherProfile (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTeacherProfile_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblTeacherProfile'))
BEGIN
    CREATE INDEX IDX_tblTeacherProfile_FamilyId ON dbo.tblTeacherProfile (FamilyId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTeacherProfile_UserId' AND object_id = OBJECT_ID(N'dbo.tblTeacherProfile'))
BEGIN
    CREATE INDEX IDX_tblTeacherProfile_UserId ON dbo.tblTeacherProfile (UserId);
END;
GO
