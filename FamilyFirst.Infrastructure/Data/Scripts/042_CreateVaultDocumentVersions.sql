IF OBJECT_ID(N'dbo.VaultDocumentVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaultDocumentVersions
    (
        VersionId             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VaultDocumentVersions PRIMARY KEY DEFAULT NEWID(),
        DocumentId            UNIQUEIDENTIFIER NOT NULL,
        FamilyId              UNIQUEIDENTIFIER NOT NULL,
        FileUrl               NVARCHAR(1000)   NOT NULL,
        VersionNumber         INT              NOT NULL,
        UploadedByUserId      UNIQUEIDENTIFIER NOT NULL,
        ArchivedAt            DATETIME2        NOT NULL CONSTRAINT DF_VaultDocumentVersions_ArchivedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultDocumentVersions_CreatedAt  DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2        NOT NULL CONSTRAINT DF_VaultDocumentVersions_UpdatedAt  DEFAULT SYSUTCDATETIME(),
        IsDeleted             BIT              NOT NULL CONSTRAINT DF_VaultDocumentVersions_IsDeleted  DEFAULT 0,
        DeletedAt             DATETIME2        NULL,

        CONSTRAINT FK_VaultDocumentVersions_VaultDocuments_DocumentId
            FOREIGN KEY (DocumentId)       REFERENCES dbo.VaultDocuments (DocumentId),
        CONSTRAINT FK_VaultDocumentVersions_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_VaultDocumentVersions_Users_UploadedByUserId
            FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users          (UserId),

        CONSTRAINT CK_VaultDocumentVersions_VersionNumber CHECK (VersionNumber >= 1)
    );
END;
GO

-- Version history lookup — DV-04 version history panel loads all versions for a document
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocumentVersions_DocumentId'
      AND object_id = OBJECT_ID(N'dbo.VaultDocumentVersions')
)
BEGIN
    CREATE INDEX IX_VaultDocumentVersions_DocumentId
        ON dbo.VaultDocumentVersions (DocumentId, VersionNumber DESC)
        WHERE IsDeleted = 0;
END;
GO

-- Row-level security filter — all queries scoped to FamilyId
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultDocumentVersions_FamilyId_IsDeleted'
      AND object_id = OBJECT_ID(N'dbo.VaultDocumentVersions')
)
BEGIN
    CREATE INDEX IX_VaultDocumentVersions_FamilyId_IsDeleted
        ON dbo.VaultDocumentVersions (FamilyId, IsDeleted)
        WHERE IsDeleted = 0;
END;
GO
