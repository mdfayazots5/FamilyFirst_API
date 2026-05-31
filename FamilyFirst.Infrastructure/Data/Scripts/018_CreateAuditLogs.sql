-- tblAuditLog is append-only. IsDeleted/DateDeleted/DeletedBy and UpdatedBy/LastUpdated
-- are omitted — justified: records are never modified or deleted; only DateCreated is needed.
-- OldValues/NewValues use NVARCHAR(MAX) — justified: arbitrary JSON audit payloads.
IF OBJECT_ID(N'dbo.tblAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAuditLog
    (
        AuditLogId      BIGINT IDENTITY(1,1) NOT NULL,
        Id              UNIQUEIDENTIFIER NOT NULL
                            CONSTRAINT DF_tblAuditLog_Id DEFAULT (NEWID()),
        CompanyId       INT NOT NULL
                            CONSTRAINT DF_tblAuditLog_CompanyId DEFAULT (1),
        SiteId          INT NOT NULL
                            CONSTRAINT DF_tblAuditLog_SiteId DEFAULT (1),

        -- Business columns
        UserId          BIGINT NULL,
        FamilyId        BIGINT NULL,
        Action          NVARCHAR(128) NOT NULL,
        EntityType      NVARCHAR(128) NOT NULL,
        EntityId        NVARCHAR(128) NOT NULL,
        OldValues       NVARCHAR(MAX) NULL,
        NewValues       NVARCHAR(MAX) NULL,
        UserAgent       NVARCHAR(512) NULL,

        -- Minimal audit columns (append-only — no update or delete columns)
        IPAddress       NVARCHAR(64) NOT NULL
                            CONSTRAINT DF_tblAuditLog_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy       NVARCHAR(128) NOT NULL
                            CONSTRAINT DF_tblAuditLog_CreatedBy DEFAULT (N'Admin'),
        DateCreated     DATETIME2 NOT NULL
                            CONSTRAINT DF_tblAuditLog_DateCreated DEFAULT (GETDATE()),

        CONSTRAINT PK_tblAuditLog_AuditLogId PRIMARY KEY (AuditLogId),
        CONSTRAINT FK_tblAuditLog_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT FK_tblAuditLog_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT CK_tblAuditLog_OldValuesJson
            CHECK (OldValues IS NULL OR ISJSON(OldValues) = 1),
        CONSTRAINT CK_tblAuditLog_NewValuesJson
            CHECK (NewValues IS NULL OR ISJSON(NewValues) = 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblAuditLog_Id' AND object_id = OBJECT_ID(N'dbo.tblAuditLog'))
BEGIN
    CREATE UNIQUE INDEX UK_tblAuditLog_Id ON dbo.tblAuditLog (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblAuditLog_FamilyId_DateCreated' AND object_id = OBJECT_ID(N'dbo.tblAuditLog'))
BEGIN
    CREATE INDEX IDX_tblAuditLog_FamilyId_DateCreated
        ON dbo.tblAuditLog (FamilyId, DateCreated);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblAuditLog_UserId' AND object_id = OBJECT_ID(N'dbo.tblAuditLog'))
BEGIN
    CREATE INDEX IDX_tblAuditLog_UserId ON dbo.tblAuditLog (UserId);
END;
GO
