-- Per-member explicit consent records for Finance module.
-- DPDP Act 2023 compliant: stores timestamp, IP, and consent version at acceptance.
-- One consent record per family member — enforced via UNIQUE index.
IF OBJECT_ID(N'dbo.tblFinanceConsent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFinanceConsent
    (
        FinanceConsentId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        FamilyMemberId          BIGINT NOT NULL,
        PrivacyTier             INT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_PrivacyTier DEFAULT (2),
        -- ConsentStatus: NotInvited / Invited / Accepted / Declined / OptedOut
        ConsentStatus           NVARCHAR(24) NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_ConsentStatus DEFAULT (N'NotInvited'),
        ConsentToken            NVARCHAR(256) NULL,
        InvitedAt               DATETIME2 NULL,
        ConsentGivenAt          DATETIME2 NULL,
        ConsentVersion          NVARCHAR(16) NULL,
        ConsentIpAddress        NVARCHAR(64) NULL,
        OptedOutAt              DATETIME2 NULL,
        LastReminderSentAt      DATETIME2 NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblFinanceConsent_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblFinanceConsent_FinanceConsentId PRIMARY KEY (FinanceConsentId),
        CONSTRAINT FK_tblFinanceConsent_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblFinanceConsent_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT CK_tblFinanceConsent_PrivacyTier
            CHECK (PrivacyTier BETWEEN 1 AND 3),
        CONSTRAINT CK_tblFinanceConsent_ConsentStatus
            CHECK (ConsentStatus IN (N'NotInvited', N'Invited', N'Accepted', N'Declined', N'OptedOut'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFinanceConsent_Id' AND object_id = OBJECT_ID(N'dbo.tblFinanceConsent'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFinanceConsent_Id ON dbo.tblFinanceConsent (Id) WHERE IsDeleted = 0;
END;
GO

-- One consent record per member
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFinanceConsent_FamilyMemberId' AND object_id = OBJECT_ID(N'dbo.tblFinanceConsent'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFinanceConsent_FamilyMemberId
        ON dbo.tblFinanceConsent (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Settings screen and consent invite flow — load all consent records per family
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFinanceConsent_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblFinanceConsent'))
BEGIN
    CREATE INDEX IDX_tblFinanceConsent_FamilyId
        ON dbo.tblFinanceConsent (FamilyId)
        WHERE IsDeleted = 0;
END;
GO

-- Consent accept flow — lookup by one-time token
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFinanceConsent_ConsentToken' AND object_id = OBJECT_ID(N'dbo.tblFinanceConsent'))
BEGIN
    CREATE INDEX IDX_tblFinanceConsent_ConsentToken
        ON dbo.tblFinanceConsent (ConsentToken)
        WHERE ConsentToken IS NOT NULL AND IsDeleted = 0;
END;
GO
