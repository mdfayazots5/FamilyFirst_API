IF OBJECT_ID(N'dbo.VaultDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaultDocuments
    (
        DocumentId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VaultDocuments PRIMARY KEY DEFAULT NEWID(),
        FamilyId              UNIQUEIDENTIFIER NOT NULL,
        MemberId              UNIQUEIDENTIFIER NOT NULL,
        UploadedByUserId      UNIQUEIDENTIFIER NOT NULL,
        DocumentName          NVARCHAR(500)    NOT NULL,
        Category              INT              NOT NULL,
        FileUrl               NVARCHAR(1000)   NOT NULL,
        ExpiryDate            DATETIME2        NULL,
        Tags                  NVARCHAR(2000)   NULL,
        IsEmergencyPriority   BIT              NOT NULL CONSTRAINT DF_VaultDocuments_IsEmergencyPriority DEFAULT 0,
        Visibility            INT              NOT NULL CONSTRAINT DF_VaultDocuments_Visibility          DEFAULT 2,
        VersionNumber         INT              NOT NULL CONSTRAINT DF_VaultDocuments_VersionNumber       DEFAULT 1,
        IsCurrentVersion      BIT              NOT NULL CONSTRAINT DF_VaultDocuments_IsCurrentVersion    DEFAULT 1,
        PermanentDeleteAt     DATETIME2        NULL,
        CreatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultDocuments_CreatedAt  DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultDocuments_UpdatedAt  DEFAULT SYSUTCDATETIME(),
        IsDeleted             BIT              NOT NULL CONSTRAINT DF_VaultDocuments_IsDeleted  DEFAULT 0,
        DeletedAt             DATETIME2        NULL,

        CONSTRAINT FK_VaultDocuments_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_VaultDocuments_FamilyMembers_MemberId
            FOREIGN KEY (MemberId)         REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT FK_VaultDocuments_Users_UploadedByUserId
            FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users         (UserId),

        CONSTRAINT CK_VaultDocuments_Category      CHECK (Category    BETWEEN 1 AND 8),
        CONSTRAINT CK_VaultDocuments_Visibility    CHECK (Visibility  BETWEEN 1 AND 4),
        CONSTRAINT CK_VaultDocuments_VersionNumber CHECK (VersionNumber >= 1)
    );
END;
GO

-- Row-level security + soft-delete filter — every query uses this
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocuments_FamilyId_IsDeleted'
      AND object_id = OBJECT_ID(N'dbo.VaultDocuments')
)
BEGIN
    CREATE INDEX IX_VaultDocuments_FamilyId_IsDeleted
        ON dbo.VaultDocuments (FamilyId, IsDeleted)
        WHERE IsDeleted = 0;
END;
GO

-- VaultExpiryWorker daily scan — ExpiryDate only present on current versions
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocuments_FamilyId_ExpiryDate'
      AND object_id = OBJECT_ID(N'dbo.VaultDocuments')
)
BEGIN
    CREATE INDEX IX_VaultDocuments_FamilyId_ExpiryDate
        ON dbo.VaultDocuments (FamilyId, ExpiryDate)
        WHERE IsDeleted = 0 AND IsCurrentVersion = 1 AND ExpiryDate IS NOT NULL;
END;
GO

-- Member-scoped document list — DV-02 category view filtered by member
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocuments_MemberId'
      AND object_id = OBJECT_ID(N'dbo.VaultDocuments')
)
BEGIN
    CREATE INDEX IX_VaultDocuments_MemberId
        ON dbo.VaultDocuments (MemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Emergency folder query — max 5 active per family enforced in service layer
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocuments_FamilyId_IsEmergencyPriority'
      AND object_id = OBJECT_ID(N'dbo.VaultDocuments')
)
BEGIN
    CREATE INDEX IX_VaultDocuments_FamilyId_IsEmergencyPriority
        ON dbo.VaultDocuments (FamilyId, IsEmergencyPriority)
        WHERE IsDeleted = 0 AND IsCurrentVersion = 1 AND IsEmergencyPriority = 1;
END;
GO
