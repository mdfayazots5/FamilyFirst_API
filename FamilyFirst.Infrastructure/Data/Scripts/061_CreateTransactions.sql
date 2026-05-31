-- Parsed SMS bank transactions.
-- Privacy tier applied at storage: Tier 2 hashes MerchantName.
-- On member opt-out: IsDeleted=1 immediately; hard DELETE after 30-day grace (DPDP Act 2023).
-- RawSmsText purged immediately on opt-out — no grace period (sensitive data).
-- CommitmentId FK is deferred — added in 064_CreateCommitments.sql after tblCommitment exists.
IF OBJECT_ID(N'dbo.tblTransaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTransaction
    (
        TransactionId           BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblTransaction_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblTransaction_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblTransaction_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        FamilyMemberId          BIGINT NOT NULL,
        -- Raw merchant name; Tier 2: hashed at render; Tier 3: not shown
        MerchantName            NVARCHAR(512) NULL,
        -- SHA-256 of MerchantName — for Tier 2 pattern detection
        MerchantNameHash        NVARCHAR(64) NULL,
        -- Positive = debit; negative = credit
        Amount                  MONEY NOT NULL,
        -- TransactionType: Debit / Credit
        TransactionType         NVARCHAR(16) NOT NULL
                                    CONSTRAINT DF_tblTransaction_TransactionType DEFAULT (N'Debit'),
        -- Category: one of 14 confirmed Indian-context categories (FinanceCategory enum)
        Category                NVARCHAR(64) NOT NULL,
        -- Immutable snapshot of privacy tier at time of capture
        PrivacyTierAtCapture    INT NOT NULL,
        IsCommitment            BIT NOT NULL
                                    CONSTRAINT DF_tblTransaction_IsCommitment DEFAULT (0),
        -- FK to tblCommitment.CommitmentId (set when matched); FK constraint added in 064
        CommitmentId            BIGINT NULL,
        -- QuestionStatus: None / Pending / FamilyExpense / Personal / UnderReview / Resolved
        QuestionStatus          NVARCHAR(24) NOT NULL
                                    CONSTRAINT DF_tblTransaction_QuestionStatus DEFAULT (N'None'),
        -- Original SMS — purged immediately on opt-out
        RawSmsText              NVARCHAR(1024) NULL,
        ParsedAt                DATETIME2 NOT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblTransaction_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblTransaction_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblTransaction_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblTransaction_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblTransaction_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblTransaction_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblTransaction_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTransaction_TransactionId PRIMARY KEY (TransactionId),
        CONSTRAINT FK_tblTransaction_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblTransaction_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT CK_tblTransaction_TransactionType
            CHECK (TransactionType IN (N'Debit', N'Credit')),
        CONSTRAINT CK_tblTransaction_PrivacyTier
            CHECK (PrivacyTierAtCapture BETWEEN 1 AND 3),
        CONSTRAINT CK_tblTransaction_QuestionStatus
            CHECK (QuestionStatus IN (N'None', N'Pending', N'FamilyExpense', N'Personal', N'UnderReview', N'Resolved'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTransaction_Id' AND object_id = OBJECT_ID(N'dbo.tblTransaction'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTransaction_Id ON dbo.tblTransaction (Id) WHERE IsDeleted = 0;
END;
GO

-- Dashboard feed + CFO transaction list — newest first, family scoped
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTransaction_FamilyId_ParsedAt' AND object_id = OBJECT_ID(N'dbo.tblTransaction'))
BEGIN
    CREATE INDEX IDX_tblTransaction_FamilyId_ParsedAt
        ON dbo.tblTransaction (FamilyId, ParsedAt DESC)
        WHERE IsDeleted = 0;
END;
GO

-- Category breakdown + per-member spend queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTransaction_FamilyMemberId_Category' AND object_id = OBJECT_ID(N'dbo.tblTransaction'))
BEGIN
    CREATE INDEX IDX_tblTransaction_FamilyMemberId_Category
        ON dbo.tblTransaction (FamilyMemberId, Category)
        WHERE IsDeleted = 0;
END;
GO
