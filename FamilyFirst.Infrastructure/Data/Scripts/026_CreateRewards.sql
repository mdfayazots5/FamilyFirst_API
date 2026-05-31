IF OBJECT_ID(N'dbo.tblReward', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblReward
    (
        RewardId                BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblReward_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblReward_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblReward_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NULL,
        MasterRewardId          BIGINT NULL,
        RewardName              NVARCHAR(256) NOT NULL,
        Description             NVARCHAR(512) NULL,
        IconCode                NVARCHAR(64) NULL,
        Category                NVARCHAR(64) NOT NULL,
        CoinCost                INT NOT NULL,
        IsSystem                BIT NOT NULL
                                    CONSTRAINT DF_tblReward_IsSystem DEFAULT (0),
        IsEnabled               BIT NOT NULL
                                    CONSTRAINT DF_tblReward_IsEnabled DEFAULT (1),
        TimesRedeemedTotal      INT NOT NULL
                                    CONSTRAINT DF_tblReward_TimesRedeemedTotal DEFAULT (0),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblReward_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblReward_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblReward_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblReward_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblReward_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblReward_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblReward_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblReward_RewardId PRIMARY KEY (RewardId),
        CONSTRAINT FK_tblReward_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblReward_MasterRewardId_tblReward_RewardId
            FOREIGN KEY (MasterRewardId) REFERENCES dbo.tblReward (RewardId),
        CONSTRAINT CK_tblReward_Category
            CHECK (Category IN (N'ScreenTime', N'FoodTreat', N'Outing', N'Purchase', N'FamilyActivity')),
        CONSTRAINT CK_tblReward_CoinCost
            CHECK (CoinCost BETWEEN 10 AND 9999),
        CONSTRAINT CK_tblReward_TimesRedeemedTotal
            CHECK (TimesRedeemedTotal >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblReward_Id' AND object_id = OBJECT_ID(N'dbo.tblReward'))
BEGIN
    CREATE UNIQUE INDEX UK_tblReward_Id ON dbo.tblReward (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblReward_FamilyId_IsEnabled' AND object_id = OBJECT_ID(N'dbo.tblReward'))
BEGIN
    CREATE INDEX IDX_tblReward_FamilyId_IsEnabled
        ON dbo.tblReward (FamilyId, IsEnabled)
        WHERE IsDeleted = 0;
END;
GO
