IF OBJECT_ID(N'dbo.RewardRedemptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RewardRedemptions
    (
        RedemptionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RewardRedemptions PRIMARY KEY DEFAULT NEWID(),
        RewardId UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        CoinsSpent INT NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_RewardRedemptions_Status DEFAULT 1,
        RequestedAt DATETIME2 NOT NULL CONSTRAINT DF_RewardRedemptions_RequestedAt DEFAULT SYSUTCDATETIME(),
        ReviewedByUserId UNIQUEIDENTIFIER NULL,
        ReviewedAt DATETIME2 NULL,
        ParentNote NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RewardRedemptions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_RewardRedemptions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_RewardRedemptions_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_RewardRedemptions_Rewards_RewardId FOREIGN KEY (RewardId) REFERENCES dbo.Rewards (RewardId),
        CONSTRAINT FK_RewardRedemptions_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_RewardRedemptions_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_RewardRedemptions_Users_ReviewedByUserId FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_RewardRedemptions_CoinsSpent CHECK (CoinsSpent >= 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_RewardRedemptions_FamilyId_Status'
        AND idx.object_id = OBJECT_ID(N'dbo.RewardRedemptions')
)
BEGIN
    CREATE INDEX IX_RewardRedemptions_FamilyId_Status
        ON dbo.RewardRedemptions
        (
            FamilyId,
            Status
        )
        WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UX_RewardRedemptions_ChildProfileId_RewardId_Pending'
        AND idx.object_id = OBJECT_ID(N'dbo.RewardRedemptions')
)
BEGIN
    CREATE UNIQUE INDEX UX_RewardRedemptions_ChildProfileId_RewardId_Pending
        ON dbo.RewardRedemptions
        (
            ChildProfileId,
            RewardId,
            Status
        )
        WHERE IsDeleted = 0 AND Status = 1;
END;
GO
