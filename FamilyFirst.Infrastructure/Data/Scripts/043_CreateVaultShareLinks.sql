IF OBJECT_ID(N'dbo.VaultShareLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaultShareLinks
    (
        ShareLinkId       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VaultShareLinks PRIMARY KEY DEFAULT NEWID(),
        DocumentId        UNIQUEIDENTIFIER NOT NULL,
        FamilyId          UNIQUEIDENTIFIER NOT NULL,
        CreatedByUserId   UNIQUEIDENTIFIER NOT NULL,
        Token             NVARCHAR(200)    NOT NULL,
        ExpiresAt         DATETIME2        NOT NULL,
        AllowDownload     BIT              NOT NULL CONSTRAINT DF_VaultShareLinks_AllowDownload DEFAULT 0,
        IsRevoked         BIT              NOT NULL CONSTRAINT DF_VaultShareLinks_IsRevoked     DEFAULT 0,
        RevokedAt         DATETIME2        NULL,
        LastAccessedAt    DATETIME2        NULL,
        CreatedAt         DATETIME2        NOT NULL CONSTRAINT DF_VaultShareLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt         DATETIME2        NOT NULL CONSTRAINT DF_VaultShareLinks_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted         BIT              NOT NULL CONSTRAINT DF_VaultShareLinks_IsDeleted DEFAULT 0,
        DeletedAt         DATETIME2        NULL,

        CONSTRAINT FK_VaultShareLinks_VaultDocuments_DocumentId
            FOREIGN KEY (DocumentId)      REFERENCES dbo.VaultDocuments (DocumentId),
        CONSTRAINT FK_VaultShareLinks_Families_FamilyId
            FOREIGN KEY (FamilyId)        REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_VaultShareLinks_Users_CreatedByUserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users          (UserId)
    );
END;
GO

-- Token lookup — every unauthenticated share-link request resolves by Token
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultShareLinks_Token'
      AND object_id = OBJECT_ID(N'dbo.VaultShareLinks')
)
BEGIN
    CREATE UNIQUE INDEX IX_VaultShareLinks_Token
        ON dbo.VaultShareLinks (Token)
        WHERE IsDeleted = 0;
END;
GO

-- Active share links per document — used to list and revoke from DV-08
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultShareLinks_DocumentId_IsRevoked'
      AND object_id = OBJECT_ID(N'dbo.VaultShareLinks')
)
BEGIN
    CREATE INDEX IX_VaultShareLinks_DocumentId_IsRevoked
        ON dbo.VaultShareLinks (DocumentId, IsRevoked)
        WHERE IsDeleted = 0;
END;
GO
