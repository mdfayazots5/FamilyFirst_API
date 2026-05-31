IF OBJECT_ID(N'dbo.tblRewardRedemption', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRewardRedemption
    (
        RewardRedemptionId  BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        RewardId            BIGINT NOT NULL,
        ChildProfileId      BIGINT NOT NULL,
        FamilyId            BIGINT NOT NULL,
        CoinsSpent          INT NOT NULL,
        Status              INT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_Status DEFAULT (1),
        RequestedAt         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_RequestedAt DEFAULT (GETDATE()),
        ReviewedByUserId    BIGINT NULL,
        ReviewedAt          DATETIME2 NULL,
        ParentNote          NVARCHAR(512) NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblRewardRedemption_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblRewardRedemption_RewardRedemptionId PRIMARY KEY (RewardRedemptionId),
        CONSTRAINT FK_tblRewardRedemption_RewardId_tblReward_RewardId
            FOREIGN KEY (RewardId) REFERENCES dbo.tblReward (RewardId),
        CONSTRAINT FK_tblRewardRedemption_ChildProfileId_tblChildProfile_ChildProfileId
            FOREIGN KEY (ChildProfileId) REFERENCES dbo.tblChildProfile (ChildProfileId),
        CONSTRAINT FK_tblRewardRedemption_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblRewardRedemption_ReviewedByUserId_tblUser_UserId
            FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblRewardRedemption_CoinsSpent
            CHECK (CoinsSpent >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblRewardRedemption_Id' AND object_id = OBJECT_ID(N'dbo.tblRewardRedemption'))
BEGIN
    CREATE UNIQUE INDEX UK_tblRewardRedemption_Id ON dbo.tblRewardRedemption (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblRewardRedemption_FamilyId_Status' AND object_id = OBJECT_ID(N'dbo.tblRewardRedemption'))
BEGIN
    CREATE INDEX IDX_tblRewardRedemption_FamilyId_Status
        ON dbo.tblRewardRedemption (FamilyId, Status)
        WHERE IsDeleted = 0;
END;
GO

-- Prevents duplicate pending redemption for same child+reward combination
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblRewardRedemption_ChildProfileId_RewardId_Pending' AND object_id = OBJECT_ID(N'dbo.tblRewardRedemption'))
BEGIN
    CREATE UNIQUE INDEX UK_tblRewardRedemption_ChildProfileId_RewardId_Pending
        ON dbo.tblRewardRedemption (ChildProfileId, RewardId, Status)
        WHERE IsDeleted = 0 AND Status = 1;
END;
GO
