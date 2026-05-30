-- Tracks which expiry reminders have already been sent per document per threshold.
-- VaultExpiryWorker checks this table before sending to avoid duplicate notifications.
IF OBJECT_ID(N'dbo.VaultExpiryReminderLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaultExpiryReminderLogs
    (
        ReminderLogId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VaultExpiryReminderLogs PRIMARY KEY DEFAULT NEWID(),
        DocumentId      UNIQUEIDENTIFIER NOT NULL,
        FamilyId        UNIQUEIDENTIFIER NOT NULL,
        ThresholdDays   INT              NOT NULL,
        SentAt          DATETIME2        NOT NULL CONSTRAINT DF_VaultExpiryReminderLogs_SentAt DEFAULT SYSUTCDATETIME(),
        CreatedAt       DATETIME2        NOT NULL CONSTRAINT DF_VaultExpiryReminderLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2        NOT NULL CONSTRAINT DF_VaultExpiryReminderLogs_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted       BIT              NOT NULL CONSTRAINT DF_VaultExpiryReminderLogs_IsDeleted DEFAULT 0,
        DeletedAt       DATETIME2        NULL,

        CONSTRAINT FK_VaultExpiryReminderLogs_VaultDocuments_DocumentId
            FOREIGN KEY (DocumentId) REFERENCES dbo.VaultDocuments (DocumentId),
        CONSTRAINT FK_VaultExpiryReminderLogs_Families_FamilyId
            FOREIGN KEY (FamilyId)   REFERENCES dbo.Families       (FamilyId),

        -- One reminder per document per threshold — worker uses this as dedup key
        CONSTRAINT UQ_VaultExpiryReminderLogs_DocumentId_ThresholdDays
            UNIQUE (DocumentId, ThresholdDays)
    );
END;
GO

-- Worker scan — find all documents that have NOT yet had a given threshold sent
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VaultExpiryReminderLogs_DocumentId_ThresholdDays'
      AND object_id = OBJECT_ID(N'dbo.VaultExpiryReminderLogs')
)
BEGIN
    CREATE INDEX IX_VaultExpiryReminderLogs_DocumentId_ThresholdDays
        ON dbo.VaultExpiryReminderLogs (DocumentId, ThresholdDays)
        WHERE IsDeleted = 0;
END;
GO
