IF OBJECT_ID(N'dbo.tblSubscription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSubscription
    (
        SubscriptionId              BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblSubscription_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblSubscription_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblSubscription_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        FamilyId                    BIGINT NOT NULL,
        PlanId                      BIGINT NOT NULL,
        Status                      NVARCHAR(24) NOT NULL,
        StartDate                   DATETIME2 NOT NULL,
        EndDate                     DATETIME2 NULL,
        TrialEndDate                DATETIME2 NULL,
        RazorpaySubscriptionId      NVARCHAR(256) NULL,
        RazorpayCustomerId          NVARCHAR(256) NULL,
        AutoRenew                   BIT NOT NULL
                                        CONSTRAINT DF_tblSubscription_AutoRenew DEFAULT (1),

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblSubscription_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblSubscription_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblSubscription_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblSubscription_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblSubscription_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblSubscription_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblSubscription_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblSubscription_SubscriptionId PRIMARY KEY (SubscriptionId),
        CONSTRAINT FK_tblSubscription_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblSubscription_PlanId_tblPlan_PlanId
            FOREIGN KEY (PlanId) REFERENCES dbo.tblPlan (PlanId),
        CONSTRAINT CK_tblSubscription_Status
            CHECK (Status IN (N'Active', N'Trial', N'Expired', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblSubscription_Id' AND object_id = OBJECT_ID(N'dbo.tblSubscription'))
BEGIN
    CREATE UNIQUE INDEX UK_tblSubscription_Id ON dbo.tblSubscription (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblSubscription_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblSubscription'))
BEGIN
    CREATE INDEX IDX_tblSubscription_FamilyId ON dbo.tblSubscription (FamilyId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblSubscription_PlanId' AND object_id = OBJECT_ID(N'dbo.tblSubscription'))
BEGIN
    CREATE INDEX IDX_tblSubscription_PlanId ON dbo.tblSubscription (PlanId);
END;
GO

-- Resolve circular dependency: tblFamily.SubscriptionId → tblSubscription
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tblFamily_SubscriptionId_tblSubscription_SubscriptionId')
BEGIN
    ALTER TABLE dbo.tblFamily
    ADD CONSTRAINT FK_tblFamily_SubscriptionId_tblSubscription_SubscriptionId
        FOREIGN KEY (SubscriptionId) REFERENCES dbo.tblSubscription (SubscriptionId);
END;
GO
