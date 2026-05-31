-- Detected recurring financial commitments (EMI, SIP, LIC, school fees, OTT, chit fund).
-- Auto-detected by NLP/regex from transaction patterns; CFO confirms via IsConfirmed flag.
-- FK from tblTransaction.CommitmentId back to this table is added below after tblCommitment exists.
IF OBJECT_ID(N'dbo.tblCommitment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCommitment
    (
        CommitmentId        BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblCommitment_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblCommitment_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblCommitment_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        FamilyId            BIGINT NOT NULL,
        -- Member whose account the commitment appears on
        FamilyMemberId      BIGINT NOT NULL,
        CommitmentName      NVARCHAR(256) NOT NULL,
        -- CommitmentType: HomeLoanEmi / SIP / LICPremium / SchoolFees / OTTSubscription / ChitFund / Other
        CommitmentType      NVARCHAR(32) NOT NULL,
        Amount              MONEY NOT NULL,
        -- Day of month (1–31)
        DueDay              INT NULL,
        -- FrequencyType: Monthly / Quarterly / Annual
        FrequencyType       NVARCHAR(24) NOT NULL
                                CONSTRAINT DF_tblCommitment_FrequencyType DEFAULT (N'Monthly'),
        NextDueDate         DATETIME2 NOT NULL,
        LastPaidAt          DATETIME2 NULL,
        -- Status: Upcoming / Paid / Missed / PendingConfirmation
        Status              NVARCHAR(24) NOT NULL
                                CONSTRAINT DF_tblCommitment_Status DEFAULT (N'Upcoming'),
        IsConfirmed         BIT NOT NULL
                                CONSTRAINT DF_tblCommitment_IsConfirmed DEFAULT (0),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblCommitment_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblCommitment_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblCommitment_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblCommitment_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblCommitment_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblCommitment_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblCommitment_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblCommitment_CommitmentId PRIMARY KEY (CommitmentId),
        CONSTRAINT FK_tblCommitment_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblCommitment_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT CK_tblCommitment_CommitmentType
            CHECK (CommitmentType IN (
                N'HomeLoanEmi', N'SIP', N'LICPremium', N'SchoolFees',
                N'OTTSubscription', N'ChitFund', N'Other'
            )),
        CONSTRAINT CK_tblCommitment_FrequencyType
            CHECK (FrequencyType IN (N'Monthly', N'Quarterly', N'Annual')),
        CONSTRAINT CK_tblCommitment_Status
            CHECK (Status IN (N'Upcoming', N'Paid', N'Missed', N'PendingConfirmation')),
        CONSTRAINT CK_tblCommitment_DueDay
            CHECK (DueDay IS NULL OR DueDay BETWEEN 1 AND 31)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCommitment_Id' AND object_id = OBJECT_ID(N'dbo.tblCommitment'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCommitment_Id ON dbo.tblCommitment (Id) WHERE IsDeleted = 0;
END;
GO

-- Dashboard commitments preview — upcoming + missed per family
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblCommitment_FamilyId_NextDueDate' AND object_id = OBJECT_ID(N'dbo.tblCommitment'))
BEGIN
    CREATE INDEX IDX_tblCommitment_FamilyId_NextDueDate
        ON dbo.tblCommitment (FamilyId, NextDueDate ASC)
        WHERE IsDeleted = 0;
END;
GO

-- Resolve circular dependency: tblTransaction.CommitmentId → tblCommitment
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tblTransaction_CommitmentId_tblCommitment_CommitmentId')
BEGIN
    ALTER TABLE dbo.tblTransaction
    ADD CONSTRAINT FK_tblTransaction_CommitmentId_tblCommitment_CommitmentId
        FOREIGN KEY (CommitmentId) REFERENCES dbo.tblCommitment (CommitmentId);
END;
GO
