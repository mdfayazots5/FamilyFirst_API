IF OBJECT_ID(N'dbo.tblFamilyMember', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFamilyMember
    (
        FamilyMemberId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblFamilyMember_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NOT NULL,
        UserId              BIGINT NOT NULL,
        Role                INT NOT NULL,
        LinkType            NVARCHAR(64) NOT NULL,
        DisplayName         NVARCHAR(256) NULL,
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_IsActive DEFAULT (1),
        JoinedAt            DATETIME2 NOT NULL
                                CONSTRAINT DF_tblFamilyMember_JoinedAt DEFAULT (GETDATE()),
        InvitedByUserId     BIGINT NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblFamilyMember_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblFamilyMember_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblFamilyMember_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblFamilyMember_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblFamilyMember_FamilyMemberId PRIMARY KEY (FamilyMemberId),
        CONSTRAINT FK_tblFamilyMember_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblFamilyMember_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblFamilyMember_InvitedByUserId_tblUser_UserId
            FOREIGN KEY (InvitedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblFamilyMember_Role
            CHECK (Role IN (1, 2, 3, 4, 5, 6))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFamilyMember_Id' AND object_id = OBJECT_ID(N'dbo.tblFamilyMember'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFamilyMember_Id ON dbo.tblFamilyMember (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFamilyMember_FamilyId_UserId' AND object_id = OBJECT_ID(N'dbo.tblFamilyMember'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFamilyMember_FamilyId_UserId
        ON dbo.tblFamilyMember (FamilyId, UserId)
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFamilyMember_UserId' AND object_id = OBJECT_ID(N'dbo.tblFamilyMember'))
BEGIN
    CREATE INDEX IDX_tblFamilyMember_UserId ON dbo.tblFamilyMember (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFamilyMember_InvitedByUserId' AND object_id = OBJECT_ID(N'dbo.tblFamilyMember'))
BEGIN
    CREATE INDEX IDX_tblFamilyMember_InvitedByUserId ON dbo.tblFamilyMember (InvitedByUserId);
END;
GO
