IF OBJECT_ID(N'dbo.CoinTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CoinTransactions
    (
        TransactionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CoinTransactions PRIMARY KEY DEFAULT NEWID(),
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        TransactionType NVARCHAR(30) NOT NULL,
        Amount INT NOT NULL,
        BalanceAfter INT NOT NULL,
        ReferenceType NVARCHAR(50) NOT NULL,
        ReferenceId UNIQUEIDENTIFIER NULL,
        Note NVARCHAR(500) NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CoinTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CoinTransactions_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_CoinTransactions_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_CoinTransactions_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_CoinTransactions_ChildProfileId_CreatedAt'
        AND idx.object_id = OBJECT_ID(N'dbo.CoinTransactions')
)
BEGIN
    CREATE INDEX IX_CoinTransactions_ChildProfileId_CreatedAt
        ON dbo.CoinTransactions
        (
            ChildProfileId,
            CreatedAt
        );
END;
GO
