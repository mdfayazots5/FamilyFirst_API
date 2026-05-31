IF OBJECT_ID(N'dbo.tblVaultDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaultDocument
    (
        VaultDocumentId         BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        FamilyMemberId          BIGINT NOT NULL,
        UploadedByUserId        BIGINT NOT NULL,
        DocumentName            NVARCHAR(512) NOT NULL,
        Category                INT NOT NULL,
        FileUrl                 NVARCHAR(1024) NOT NULL,
        ExpiryDate              DATETIME2 NULL,
        Tags                    NVARCHAR(2048) NULL,
        IsEmergencyPriority     BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_IsEmergencyPriority DEFAULT (0),
        Visibility              INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_Visibility DEFAULT (2),
        VersionNumber           INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_VersionNumber DEFAULT (1),
        IsCurrentVersion        BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_IsCurrentVersion DEFAULT (1),
        PermanentDeleteAt       DATETIME2 NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblVaultDocument_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaultDocument_VaultDocumentId PRIMARY KEY (VaultDocumentId),
        CONSTRAINT FK_tblVaultDocument_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblVaultDocument_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT FK_tblVaultDocument_UploadedByUserId_tblUser_UserId
            FOREIGN KEY (UploadedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblVaultDocument_Category     CHECK (Category    BETWEEN 1 AND 8),
        CONSTRAINT CK_tblVaultDocument_Visibility   CHECK (Visibility  BETWEEN 1 AND 4),
        CONSTRAINT CK_tblVaultDocument_VersionNumber CHECK (VersionNumber >= 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultDocument_Id' AND object_id = OBJECT_ID(N'dbo.tblVaultDocument'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultDocument_Id ON dbo.tblVaultDocument (Id) WHERE IsDeleted = 0;
END;
GO

-- Row-level security + soft-delete filter — every query uses this
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocument_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblVaultDocument'))
BEGIN
    CREATE INDEX IDX_tblVaultDocument_FamilyId
        ON dbo.tblVaultDocument (FamilyId)
        WHERE IsDeleted = 0;
END;
GO

-- VaultExpiryWorker daily scan — ExpiryDate only present on current versions
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocument_FamilyId_ExpiryDate' AND object_id = OBJECT_ID(N'dbo.tblVaultDocument'))
BEGIN
    CREATE INDEX IDX_tblVaultDocument_FamilyId_ExpiryDate
        ON dbo.tblVaultDocument (FamilyId, ExpiryDate)
        WHERE IsDeleted = 0 AND IsCurrentVersion = 1 AND ExpiryDate IS NOT NULL;
END;
GO

-- Member-scoped document list — DV-02 category view filtered by member
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocument_FamilyMemberId' AND object_id = OBJECT_ID(N'dbo.tblVaultDocument'))
BEGIN
    CREATE INDEX IDX_tblVaultDocument_FamilyMemberId
        ON dbo.tblVaultDocument (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Emergency folder query — max 5 active per family enforced in service layer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultDocument_FamilyId_IsEmergencyPriority' AND object_id = OBJECT_ID(N'dbo.tblVaultDocument'))
BEGIN
    CREATE INDEX IDX_tblVaultDocument_FamilyId_IsEmergencyPriority
        ON dbo.tblVaultDocument (FamilyId, IsEmergencyPriority)
        WHERE IsDeleted = 0 AND IsCurrentVersion = 1 AND IsEmergencyPriority = 1;
END;
GO
