-- Per-member explicit consent records for Finance module.
-- DPDP Act 2023 compliant: stores timestamp, IP, and consent version at acceptance.
-- One consent record per family member — enforced via UNIQUE index.
IF OBJECT_ID(N'dbo.FinanceConsents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinanceConsents
    (
        FinanceConsentId        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FinanceConsents PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId          UNIQUEIDENTIFIER NOT NULL,
        PrivacyTier             INT              NOT NULL CONSTRAINT DF_FinanceConsents_PrivacyTier DEFAULT 2,
        -- ConsentStatus: NotInvited / Invited / Accepted / Declined / OptedOut
        ConsentStatus           NVARCHAR(20)     NOT NULL CONSTRAINT DF_FinanceConsents_ConsentStatus DEFAULT N'NotInvited',
        ConsentToken            NVARCHAR(200)    NULL,       -- One-time token sent in consent invite SMS link
        InvitedAt               DATETIME2        NULL,
        ConsentGivenAt          DATETIME2        NULL,
        ConsentVersion          NVARCHAR(10)     NULL,       -- e.g. "v1.2" — legal version at acceptance
        ConsentIpAddress        NVARCHAR(45)     NULL,       -- IPv4/IPv6 captured server-side at acceptance
        OptedOutAt              DATETIME2        NULL,
        LastReminderSentAt      DATETIME2        NULL,       -- Monthly reminder SMS scheduling
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_FinanceConsents_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_FinanceConsents_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_FinanceConsents_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_FinanceConsents_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_FinanceConsents_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT CK_FinanceConsents_PrivacyTier
            CHECK (PrivacyTier BETWEEN 1 AND 3),
        CONSTRAINT CK_FinanceConsents_ConsentStatus
            CHECK (ConsentStatus IN (N'NotInvited', N'Invited', N'Accepted', N'Declined', N'OptedOut'))
    );
END;
GO

-- One consent record per member
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_FinanceConsents_FamilyMemberId'
      AND object_id = OBJECT_ID(N'dbo.FinanceConsents')
)
BEGIN
    CREATE UNIQUE INDEX UX_FinanceConsents_FamilyMemberId
        ON dbo.FinanceConsents (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Settings screen and consent invite flow — load all consent records per family
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FinanceConsents_FamilyId'
      AND object_id = OBJECT_ID(N'dbo.FinanceConsents')
)
BEGIN
    CREATE INDEX IX_FinanceConsents_FamilyId
        ON dbo.FinanceConsents (FamilyId)
        WHERE IsDeleted = 0;
END;
GO

-- Consent accept flow — lookup by one-time token
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FinanceConsents_ConsentToken'
      AND object_id = OBJECT_ID(N'dbo.FinanceConsents')
)
BEGIN
    CREATE INDEX IX_FinanceConsents_ConsentToken
        ON dbo.FinanceConsents (ConsentToken)
        WHERE ConsentToken IS NOT NULL AND IsDeleted = 0;
END;
GO
