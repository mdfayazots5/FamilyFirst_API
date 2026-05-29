IF OBJECT_ID(N'dbo.Subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subscriptions
    (
        SubscriptionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Subscriptions PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        PlanId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        TrialEndDate DATE NULL,
        RazorpaySubscriptionId NVARCHAR(200) NULL,
        RazorpayCustomerId NVARCHAR(200) NULL,
        AutoRenew BIT NOT NULL CONSTRAINT DF_Subscriptions_AutoRenew DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Subscriptions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Subscriptions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Subscriptions_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_Subscriptions_Plans_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.Plans (PlanId),
        CONSTRAINT CK_Subscriptions_Status CHECK (Status IN (N'Active', N'Trial', N'Expired', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_FamilyId' AND object_id = OBJECT_ID(N'dbo.Subscriptions'))
BEGIN
    CREATE INDEX IX_Subscriptions_FamilyId ON dbo.Subscriptions (FamilyId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_PlanId' AND object_id = OBJECT_ID(N'dbo.Subscriptions'))
BEGIN
    CREATE INDEX IX_Subscriptions_PlanId ON dbo.Subscriptions (PlanId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Families_Subscriptions_SubscriptionId')
BEGIN
    ALTER TABLE dbo.Families
    ADD CONSTRAINT FK_Families_Subscriptions_SubscriptionId
        FOREIGN KEY (SubscriptionId) REFERENCES dbo.Subscriptions (SubscriptionId);
END;
GO
