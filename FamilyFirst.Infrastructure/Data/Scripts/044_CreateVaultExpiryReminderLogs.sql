-- Tracks which expiry reminders have already been sent per document per threshold.
-- VaultExpiryWorker checks this table before sending to avoid duplicate notifications.
IF OBJECT_ID(N'dbo.tblVaultExpiryReminderLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblVaultExpiryReminderLog
    (
        VaultExpiryReminderLogId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        VaultDocumentId             BIGINT NOT NULL,
        FamilyId                    BIGINT NOT NULL,
        ThresholdDays               INT NOT NULL,
        SentAt                      DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_SentAt DEFAULT (GETDATE()),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblVaultExpiryReminderLog_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblVaultExpiryReminderLog_VaultExpiryReminderLogId
            PRIMARY KEY (VaultExpiryReminderLogId),
        CONSTRAINT FK_tblVaultExpiryReminderLog_VaultDocumentId_tblVaultDocument_VaultDocumentId
            FOREIGN KEY (VaultDocumentId) REFERENCES dbo.tblVaultDocument (VaultDocumentId),
        CONSTRAINT FK_tblVaultExpiryReminderLog_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        -- One reminder per document per threshold — worker uses this as dedup key
        CONSTRAINT UK_tblVaultExpiryReminderLog_VaultDocumentId_ThresholdDays
            UNIQUE (VaultDocumentId, ThresholdDays)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblVaultExpiryReminderLog_Id' AND object_id = OBJECT_ID(N'dbo.tblVaultExpiryReminderLog'))
BEGIN
    CREATE UNIQUE INDEX UK_tblVaultExpiryReminderLog_Id
        ON dbo.tblVaultExpiryReminderLog (Id) WHERE IsDeleted = 0;
END;
GO

-- Worker scan — find all documents that have NOT yet had a given threshold sent
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblVaultExpiryReminderLog_VaultDocumentId_ThresholdDays' AND object_id = OBJECT_ID(N'dbo.tblVaultExpiryReminderLog'))
BEGIN
    CREATE INDEX IDX_tblVaultExpiryReminderLog_VaultDocumentId_ThresholdDays
        ON dbo.tblVaultExpiryReminderLog (VaultDocumentId, ThresholdDays)
        WHERE IsDeleted = 0;
END;
GO
