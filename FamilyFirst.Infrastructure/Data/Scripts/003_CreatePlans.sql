IF OBJECT_ID(N'dbo.tblPlan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblPlan
    (
        PlanId              BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblPlan_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblPlan_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblPlan_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        PlanName            NVARCHAR(128) NOT NULL,
        PlanCode            NVARCHAR(64) NOT NULL,
        PriceMonthly        MONEY NOT NULL,
        MaxChildren         INT NOT NULL,
        MaxTeachers         INT NOT NULL,
        HasElderMode        BIT NOT NULL
                                CONSTRAINT DF_tblPlan_HasElderMode DEFAULT (0),
        HasWeeklyDigest     BIT NOT NULL
                                CONSTRAINT DF_tblPlan_HasWeeklyDigest DEFAULT (0),
        HasAdvancedReports  BIT NOT NULL
                                CONSTRAINT DF_tblPlan_HasAdvancedReports DEFAULT (0),
        StorageQuotaMb      INT NOT NULL
                                CONSTRAINT DF_tblPlan_StorageQuotaMb DEFAULT (0),
        TrialDays           INT NOT NULL
                                CONSTRAINT DF_tblPlan_TrialDays DEFAULT (0),
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblPlan_IsActive DEFAULT (1),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblPlan_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblPlan_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblPlan_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblPlan_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblPlan_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblPlan_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblPlan_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblPlan_PlanId PRIMARY KEY (PlanId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblPlan_Id' AND object_id = OBJECT_ID(N'dbo.tblPlan'))
BEGIN
    CREATE UNIQUE INDEX UK_tblPlan_Id ON dbo.tblPlan (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblPlan_PlanName' AND object_id = OBJECT_ID(N'dbo.tblPlan'))
BEGIN
    CREATE UNIQUE INDEX UK_tblPlan_PlanName ON dbo.tblPlan (PlanName);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblPlan_PlanCode' AND object_id = OBJECT_ID(N'dbo.tblPlan'))
BEGIN
    CREATE UNIQUE INDEX UK_tblPlan_PlanCode ON dbo.tblPlan (PlanCode);
END;
GO
