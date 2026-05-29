IF OBJECT_ID(N'dbo.Rewards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rewards
    (
        RewardId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Rewards PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NULL,
        MasterRewardId UNIQUEIDENTIFIER NULL,
        RewardName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IconCode NVARCHAR(50) NULL,
        Category NVARCHAR(50) NOT NULL,
        CoinCost INT NOT NULL,
        IsSystem BIT NOT NULL CONSTRAINT DF_Rewards_IsSystem DEFAULT 0,
        IsEnabled BIT NOT NULL CONSTRAINT DF_Rewards_IsEnabled DEFAULT 1,
        TimesRedeemedTotal INT NOT NULL CONSTRAINT DF_Rewards_TimesRedeemedTotal DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Rewards_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Rewards_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Rewards_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_Rewards_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_Rewards_Rewards_MasterRewardId FOREIGN KEY (MasterRewardId) REFERENCES dbo.Rewards (RewardId),
        CONSTRAINT CK_Rewards_Category CHECK (Category IN (N'ScreenTime', N'FoodTreat', N'Outing', N'Purchase', N'FamilyActivity')),
        CONSTRAINT CK_Rewards_CoinCost CHECK (CoinCost BETWEEN 10 AND 9999),
        CONSTRAINT CK_Rewards_TimesRedeemedTotal CHECK (TimesRedeemedTotal >= 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_Rewards_FamilyId_IsEnabled'
        AND idx.object_id = OBJECT_ID(N'dbo.Rewards')
)
BEGIN
    CREATE INDEX IX_Rewards_FamilyId_IsEnabled
        ON dbo.Rewards
        (
            FamilyId,
            IsEnabled
        )
        WHERE IsDeleted = 0;
END;
GO
