IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NULL,
        FamilyId UNIQUEIDENTIFIER NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AuditLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_AuditLogs_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_AuditLogs_OldValuesJson CHECK (OldValues IS NULL OR ISJSON(OldValues) = 1),
        CONSTRAINT CK_AuditLogs_NewValuesJson CHECK (NewValues IS NULL OR ISJSON(NewValues) = 1)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AuditLogs_FamilyId_CreatedAt'
        AND idx.object_id = OBJECT_ID(N'dbo.AuditLogs')
)
BEGIN
    CREATE INDEX IX_AuditLogs_FamilyId_CreatedAt
        ON dbo.AuditLogs
        (
            FamilyId,
            CreatedAt
        );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AuditLogs_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.AuditLogs')
)
BEGIN
    CREATE INDEX IX_AuditLogs_UserId
        ON dbo.AuditLogs
        (
            UserId
        );
END;
GO
