-- tblCoinTransaction is append-only. UpdatedBy/LastUpdated/DeletedBy/DateDeleted/IsDeleted
-- are omitted — justified: coin ledger records are never modified or deleted.
-- ReferenceId is a soft (non-FK) reference to the triggering entity's BIGINT PK;
-- the referenced table varies based on ReferenceType (TaskCompletion, RewardRedemption, etc.).
IF OBJECT_ID(N'dbo.tblCoinTransaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCoinTransaction
    (
        CoinTransactionId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_SiteId DEFAULT (1),

        -- Business columns
        ChildProfileId      BIGINT NOT NULL,
        FamilyId            BIGINT NOT NULL,
        TransactionType     NVARCHAR(32) NOT NULL,
        Amount              INT NOT NULL,
        BalanceAfter        INT NOT NULL,
        ReferenceType       NVARCHAR(64) NOT NULL,
        ReferenceId         BIGINT NULL,
        Note                NVARCHAR(512) NULL,
        CreatedByUserId     BIGINT NOT NULL,

        -- Minimal audit columns (append-only — no update or delete columns)
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblCoinTransaction_DateCreated DEFAULT (GETDATE()),

        CONSTRAINT PK_tblCoinTransaction_CoinTransactionId PRIMARY KEY (CoinTransactionId),
        CONSTRAINT FK_tblCoinTransaction_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblCoinTransaction_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblCoinTransaction_CreatedByUserId_tblUser_UserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCoinTransaction_Id' AND object_id = OBJECT_ID(N'dbo.tblCoinTransaction'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCoinTransaction_Id ON dbo.tblCoinTransaction (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblCoinTransaction_ChildProfileId_DateCreated' AND object_id = OBJECT_ID(N'dbo.tblCoinTransaction'))
BEGIN
    CREATE INDEX IDX_tblCoinTransaction_ChildProfileId_DateCreated
        ON dbo.tblCoinTransaction (ChildProfileId, DateCreated);
END;
GO
