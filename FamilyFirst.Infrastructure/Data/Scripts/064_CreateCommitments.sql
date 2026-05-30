-- Detected recurring financial commitments (EMI, SIP, LIC, school fees, OTT, chit fund).
-- Auto-detected by NLP/regex from transaction patterns; CFO confirms via IsConfirmed flag.
-- FK to Transactions.CommitmentId added after this table exists.
IF OBJECT_ID(N'dbo.Commitments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Commitments
    (
        CommitmentId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Commitments PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId          UNIQUEIDENTIFIER NOT NULL,   -- Member whose account
        CommitmentName          NVARCHAR(200)    NOT NULL,   -- e.g. "HDFC Home Loan EMI"
        -- CommitmentType: HomeLoanEmi / SIP / LICPremium / SchoolFees / OTTSubscription / ChitFund / Other
        CommitmentType          NVARCHAR(30)     NOT NULL,
        Amount                  DECIMAL(18,2)    NOT NULL,
        DueDay                  INT              NULL,        -- Day of month (1–31)
        -- FrequencyType: Monthly / Quarterly / Annual
        FrequencyType           NVARCHAR(20)     NOT NULL CONSTRAINT DF_Commitments_FrequencyType DEFAULT N'Monthly',
        NextDueDate             DATE             NOT NULL,
        LastPaidAt              DATETIME2        NULL,
        -- Status: Upcoming / Paid / Missed / PendingConfirmation
        Status                  NVARCHAR(20)     NOT NULL CONSTRAINT DF_Commitments_Status DEFAULT N'Upcoming',
        IsConfirmed             BIT              NOT NULL CONSTRAINT DF_Commitments_IsConfirmed DEFAULT 0,
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Commitments_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Commitments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_Commitments_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_Commitments_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_Commitments_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT CK_Commitments_CommitmentType
            CHECK (CommitmentType IN (
                N'HomeLoanEmi', N'SIP', N'LICPremium', N'SchoolFees',
                N'OTTSubscription', N'ChitFund', N'Other'
            )),
        CONSTRAINT CK_Commitments_FrequencyType
            CHECK (FrequencyType IN (N'Monthly', N'Quarterly', N'Annual')),
        CONSTRAINT CK_Commitments_Status
            CHECK (Status IN (N'Upcoming', N'Paid', N'Missed', N'PendingConfirmation')),
        CONSTRAINT CK_Commitments_DueDay
            CHECK (DueDay IS NULL OR DueDay BETWEEN 1 AND 31)
    );
END;
GO

-- Dashboard commitments preview — upcoming + missed per family
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Commitments_FamilyId_NextDueDate'
      AND object_id = OBJECT_ID(N'dbo.Commitments')
)
BEGIN
    CREATE INDEX IX_Commitments_FamilyId_NextDueDate
        ON dbo.Commitments (FamilyId, NextDueDate ASC)
        WHERE IsDeleted = 0;
END;
GO

-- Add FK from Transactions to Commitments now that Commitments table exists
IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Transactions_Commitments_CommitmentId'
)
BEGIN
    ALTER TABLE dbo.Transactions
    ADD CONSTRAINT FK_Transactions_Commitments_CommitmentId
        FOREIGN KEY (CommitmentId) REFERENCES dbo.Commitments (CommitmentId);
END;
GO
