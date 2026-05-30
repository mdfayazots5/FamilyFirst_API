-- Per-category monthly budget targets set by Family CFO.
-- One row per family + category + month — enforced via UNIQUE index.
IF OBJECT_ID(N'dbo.Budgets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Budgets
    (
        BudgetId                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Budgets PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        Category                NVARCHAR(50)     NOT NULL,   -- One of 14 confirmed FinanceCategory values
        MonthYear               DATE             NOT NULL,   -- First day of month: e.g. 2026-04-01
        BudgetAmount            DECIMAL(18,2)    NOT NULL,
        SetByUserId             UNIQUEIDENTIFIER NOT NULL,   -- FK → Users.UserId (CFO)
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Budgets_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_Budgets_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_Budgets_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_Budgets_Families_FamilyId
            FOREIGN KEY (FamilyId)    REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_Budgets_Users_SetByUserId
            FOREIGN KEY (SetByUserId) REFERENCES dbo.Users    (UserId)
    );
END;
GO

-- One budget entry per family + category + month
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Budgets_FamilyId_Category_MonthYear'
      AND object_id = OBJECT_ID(N'dbo.Budgets')
)
BEGIN
    CREATE UNIQUE INDEX UX_Budgets_FamilyId_Category_MonthYear
        ON dbo.Budgets (FamilyId, Category, MonthYear)
        WHERE IsDeleted = 0;
END;
GO
