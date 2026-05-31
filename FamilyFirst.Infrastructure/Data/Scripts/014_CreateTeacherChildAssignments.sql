IF OBJECT_ID(N'dbo.tblTeacherChildAssignment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTeacherChildAssignment
    (
        TeacherChildAssignmentId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        TeacherProfileId            BIGINT NOT NULL,
        ChildProfileId              BIGINT NOT NULL,
        FamilyId                    BIGINT NOT NULL,
        AssignedAt                  DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_AssignedAt DEFAULT (GETDATE()),
        IsActive                    BIT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_IsActive DEFAULT (1),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblTeacherChildAssignment_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTeacherChildAssignment_TeacherChildAssignmentId
            PRIMARY KEY (TeacherChildAssignmentId),
        CONSTRAINT FK_tblTeacherChildAssignment_TeacherProfileId_tblTeacherProfile_TeacherProfileId
            FOREIGN KEY (TeacherProfileId) REFERENCES dbo.tblTeacherProfile (TeacherProfileId),
        CONSTRAINT FK_tblTeacherChildAssignment_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblTeacherChildAssignment_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTeacherChildAssignment_Id' AND object_id = OBJECT_ID(N'dbo.tblTeacherChildAssignment'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTeacherChildAssignment_Id
        ON dbo.tblTeacherChildAssignment (Id) WHERE IsDeleted = 0;
END;
GO

-- Prevents duplicate active assignment of same teacher to same child
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTeacherChildAssignment_TeacherProfileId_ChildProfileId' AND object_id = OBJECT_ID(N'dbo.tblTeacherChildAssignment'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTeacherChildAssignment_TeacherProfileId_ChildProfileId
        ON dbo.tblTeacherChildAssignment (TeacherProfileId, ChildProfileId)
        WHERE IsActive = 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTeacherChildAssignment_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblTeacherChildAssignment'))
BEGIN
    CREATE INDEX IDX_tblTeacherChildAssignment_FamilyId
        ON dbo.tblTeacherChildAssignment (FamilyId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTeacherChildAssignment_ChildProfileId' AND object_id = OBJECT_ID(N'dbo.tblTeacherChildAssignment'))
BEGIN
    CREATE INDEX IDX_tblTeacherChildAssignment_ChildProfileId
        ON dbo.tblTeacherChildAssignment (ChildProfileId);
END;
GO
