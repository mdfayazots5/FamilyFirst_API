-- Parsed SMS bank transactions.
-- Privacy tier applied at storage: Tier 2 hashes MerchantName.
-- On member opt-out: IsDeleted=1 immediately; hard DELETE after 30-day grace (DPDP Act 2023).
-- RawSmsText purged immediately on opt-out — no grace period (sensitive data).
IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        TransactionId           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId          UNIQUEIDENTIFIER NOT NULL,
        MerchantName            NVARCHAR(300)    NULL,       -- Raw merchant name; Tier 2: hashed at render; Tier 3: not shown
        MerchantNameHash        NVARCHAR(64)     NULL,       -- SHA-256 of MerchantName — for Tier 2 pattern detection
        Amount                  DECIMAL(18,2)    NOT NULL,   -- Positive = debit; negative = credit
        -- TransactionType: Debit / Credit
        TransactionType         NVARCHAR(10)     NOT NULL CONSTRAINT DF_Transactions_TransactionType DEFAULT N'Debit',
        -- Category: one of 14 confirmed Indian-context categories (FinanceCategory enum)
        Category                NVARCHAR(50)     NOT NULL,
        PrivacyTierAtCapture    INT              NOT NULL,   -- Immutable snapshot of tier at time of capture
        IsCommitment            BIT              NOT NULL CONSTRAINT DF_Transactions_IsCommitment DEFAULT 0,
        CommitmentId            UNIQUEIDENTIFIER NULL,       -- FK → Commitments.CommitmentId (set when matched)
        -- QuestionStatus: None / Pending / FamilyExpense / Personal / UnderReview / Resolved
        QuestionStatus          NVARCHAR(20)     NOT NULL CONSTRAINT DF_Transactions_QuestionStatus DEFAULT N'None',
        RawSmsText              NVARCHAR(1000)   NULL,       -- Original SMS — purged immediately on opt-out
        ParsedAt                DATETIME2        NOT NULL,   -- UTC timestamp of SMS receipt/parse
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Transactions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_Transactions_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_Transactions_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_Transactions_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT CK_Transactions_TransactionType
            CHECK (TransactionType IN (N'Debit', N'Credit')),
        CONSTRAINT CK_Transactions_PrivacyTier
            CHECK (PrivacyTierAtCapture BETWEEN 1 AND 3),
        CONSTRAINT CK_Transactions_QuestionStatus
            CHECK (QuestionStatus IN (N'None', N'Pending', N'FamilyExpense', N'Personal', N'UnderReview', N'Resolved'))
    );
END;
GO

-- Dashboard feed + CFO transaction list — newest first, family scoped
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Transactions_FamilyId_ParsedAt'
      AND object_id = OBJECT_ID(N'dbo.Transactions')
)
BEGIN
    CREATE INDEX IX_Transactions_FamilyId_ParsedAt
        ON dbo.Transactions (FamilyId, ParsedAt DESC)
        WHERE IsDeleted = 0;
END;
GO

-- Category breakdown + per-member spend queries
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Transactions_FamilyMemberId_Category'
      AND object_id = OBJECT_ID(N'dbo.Transactions')
)
BEGIN
    CREATE INDEX IX_Transactions_FamilyMemberId_Category
        ON dbo.Transactions (FamilyMemberId, Category)
        WHERE IsDeleted = 0;
END;
GO
