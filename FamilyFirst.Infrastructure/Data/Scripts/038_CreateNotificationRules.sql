IF OBJECT_ID(N'dbo.tblNotificationRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblNotificationRule
    (
        NotificationRuleId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        RuleKey                 NVARCHAR(64) NOT NULL,
        IsEnabled               BIT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_IsEnabled DEFAULT (1),
        PriorityOverride        INT NULL,
        DeliveryDelayMinutes    INT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblNotificationRule_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblNotificationRule_NotificationRuleId PRIMARY KEY (NotificationRuleId),
        CONSTRAINT FK_tblNotificationRule_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblNotificationRule_Id' AND object_id = OBJECT_ID(N'dbo.tblNotificationRule'))
BEGIN
    CREATE UNIQUE INDEX UK_tblNotificationRule_Id ON dbo.tblNotificationRule (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblNotificationRule_FamilyId_RuleKey' AND object_id = OBJECT_ID(N'dbo.tblNotificationRule'))
BEGIN
    CREATE UNIQUE INDEX UK_tblNotificationRule_FamilyId_RuleKey
        ON dbo.tblNotificationRule (FamilyId, RuleKey);
END;
GO
