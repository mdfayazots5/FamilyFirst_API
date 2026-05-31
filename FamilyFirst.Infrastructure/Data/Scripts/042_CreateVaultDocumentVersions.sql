IF OBJECT_ID(N'dbo.tblVaultDocumentVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaultDocumentVersion
    (
        VaultDocumentVersionId  BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        VaultDocumentId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        FileUrl                 NVARCHAR(1024) NOT NULL,
        VersionNumber           INT NOT NULL,
        UploadedByUserId        BIGINT NOT NULL,
        ArchivedAt              DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_ArchivedAt DEFAULT (GETDATE()),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocumentVersion_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaultDocumentVersion_VaultDocumentVersionId
            PRIMARY KEY (VaultDocumentVersionId),
        CONSTRAINT FK_tblVaultDocumentVersion_VaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (VaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT FK_tblVaultDocumentVersion_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblVaultDocumentVersion_UploadedByUserId_tblUser_UserId
            FOREIGN KEY (UploadedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblVaultDocumentVersion_VersionNumber CHECK (VersionNumber >= 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultDocumentVersion_Id' AND object_id = OBJECT_ID(N'dbo.tblVaultDocumentVersion'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultDocumentVersion_Id
        ON dbo.tblVaultDocumentVersion (Id) WHERE IsDeleted = 0;
END;
GO

-- Version history lookup — DV-04 version history panel loads all versions for a document
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocumentVersion_VaultDocumentId' AND object_id = OBJECT_ID(N'dbo.tblVaultDocumentVersion'))
BEGIN
    CREATE INDEX IDX_tblVaultDocumentVersion_VaultDocumentId
        ON dbo.tblVaultDocumentVersion (VaultDocumentId, VersionNumber DESC)
        WHERE IsDeleted = 0;
END;
GO

-- Row-level security filter — all queries scoped to FamilyId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocumentVersion_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblVaultDocumentVersion'))
BEGIN
    CREATE INDEX IDX_tblVaultDocumentVersion_FamilyId
        ON dbo.tblVaultDocumentVersion (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
