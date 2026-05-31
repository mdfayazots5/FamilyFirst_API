IF OBJECT_ID(N'dbo.tblVaultShareLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaultShareLink
    (
        VaultShareLinkId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        VaultDocumentId     BIGINT NOT NULL,
        FamilyId            BIGINT NOT NULL,
        CreatedByUserId     BIGINT NOT NULL,
        Token               NVARCHAR(256) NOT NULL,
        ExpiresAt           DATETIME2 NOT NULL,
        AllowDownload       BIT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_AllowDownload DEFAULT (0),
        IsRevoked           BIT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_IsRevoked DEFAULT (0),
        RevokedAt           DATETIME2 NULL,
        LastAccessedAt      DATETIME2 NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblVaultShareLink_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaultShareLink_VaultShareLinkId PRIMARY KEY (VaultShareLinkId),
        CONSTRAINT FK_tblVaultShareLink_VaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (VaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT FK_tblVaultShareLink_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblVaultShareLink_CreatedByUserId_tblUser_UserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultShareLink_Id' AND object_id = OBJECT_ID(N'dbo.tblVaultShareLink'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultShareLink_Id ON dbo.tblVaultShareLink (Id) WHERE IsDeleted = 0;
END;
GO

-- Token lookup — every unauthenticated share-link request resolves by Token
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultShareLink_Token' AND object_id = OBJECT_ID(N'dbo.tblVaultShareLink'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultShareLink_Token
        ON dbo.tblVaultShareLink (Token)
        WHERE IsDeleted = 0;
END;
GO

-- Active share links per document — used to list and revoke from DV-08
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultShareLink_VaultDocumentId_IsRevoked' AND object_id = OBJECT_ID(N'dbo.tblVaultShareLink'))
BEGIN
    CREATE INDEX IDX_tblVaultShareLink_VaultDocumentId_IsRevoked
        ON dbo.tblVaultShareLink (VaultDocumentId, IsRevoked)
        WHERE IsDeleted = 0;
END;
GO
