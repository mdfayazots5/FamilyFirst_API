IF OBJECT_ID(N'dbo.FeatureFlags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeatureFlags
    (
        FlagKey NVARCHAR(100) NOT NULL CONSTRAINT PK_FeatureFlags PRIMARY KEY,
        FlagValue NVARCHAR(200) NOT NULL,
        Description NVARCHAR(300) NULL,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_FeatureFlags_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO
