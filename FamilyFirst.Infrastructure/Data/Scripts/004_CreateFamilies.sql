IF OBJECT_ID(N'dbo.tblFamily', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblFamily
    (
        FamilyId                BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblFamily_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblFamily_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblFamily_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyName              NVARCHAR(256) NOT NULL,
        JoinCode                NVARCHAR(16) NOT NULL,
        City                    NVARCHAR(128) NULL,
        PlanId                  BIGINT NOT NULL,
        SubscriptionId          BIGINT NULL,
        FamilyAdminUserId       BIGINT NOT NULL,
        FamilyScore             INT NOT NULL
                                    CONSTRAINT DF_tblFamily_FamilyScore DEFAULT (0),
        FamilyScoreUpdatedAt    DATETIME2 NULL,
        CurrentStreakDays       INT NOT NULL
                                    CONSTRAINT DF_tblFamily_CurrentStreakDays DEFAULT (0),
        BestStreakDays          INT NOT NULL
                                    CONSTRAINT DF_tblFamily_BestStreakDays DEFAULT (0),
        TimezoneId              NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblFamily_TimezoneId DEFAULT (N'Asia/Kolkata'),
        IsActive                BIT NOT NULL
                                    CONSTRAINT DF_tblFamily_IsActive DEFAULT (1),

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblFamily_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblFamily_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblFamily_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblFamily_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblFamily_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblFamily_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblFamily_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblFamily_FamilyId PRIMARY KEY (FamilyId),
        CONSTRAINT FK_tblFamily_PlanId_tblPlan_PlanId
            FOREIGN KEY (PlanId) REFERENCES dbo.tblPlan (PlanId),
        CONSTRAINT FK_tblFamily_FamilyAdminUserId_tblUser_UserId
            FOREIGN KEY (FamilyAdminUserId) REFERENCES dbo.tblUser (UserId)
        -- FK_tblFamily_SubscriptionId_tblSubscription_SubscriptionId added in 005_CreateSubscriptions.sql
        -- after tblSubscription is created (circular dependency)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFamily_Id' AND object_id = OBJECT_ID(N'dbo.tblFamily'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFamily_Id ON dbo.tblFamily (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblFamily_JoinCode' AND object_id = OBJECT_ID(N'dbo.tblFamily'))
BEGIN
    CREATE UNIQUE INDEX UK_tblFamily_JoinCode ON dbo.tblFamily (JoinCode) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFamily_PlanId' AND object_id = OBJECT_ID(N'dbo.tblFamily'))
BEGIN
    CREATE INDEX IDX_tblFamily_PlanId ON dbo.tblFamily (PlanId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblFamily_FamilyAdminUserId' AND object_id = OBJECT_ID(N'dbo.tblFamily'))
BEGIN
    CREATE INDEX IDX_tblFamily_FamilyAdminUserId ON dbo.tblFamily (FamilyAdminUserId);
END;
GO
