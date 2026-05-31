IF OBJECT_ID(N'dbo.tblRefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblRefreshToken
    (
        RefreshTokenId      BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblRefreshToken_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        UserId              BIGINT NOT NULL,
        Token               NVARCHAR(512) NOT NULL,
        DeviceInfo          NVARCHAR(512) NULL,
        ExpiresAt           DATETIME2 NOT NULL,
        IsRevoked           BIT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_IsRevoked DEFAULT (0),

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblRefreshToken_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblRefreshToken_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblRefreshToken_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblRefreshToken_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblRefreshToken_RefreshTokenId PRIMARY KEY (RefreshTokenId),
        CONSTRAINT FK_tblRefreshToken_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblRefreshToken_Id' AND object_id = OBJECT_ID(N'dbo.tblRefreshToken'))
BEGIN
    CREATE UNIQUE INDEX UK_tblRefreshToken_Id ON dbo.tblRefreshToken (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblRefreshToken_Token' AND object_id = OBJECT_ID(N'dbo.tblRefreshToken'))
BEGIN
    CREATE UNIQUE INDEX UK_tblRefreshToken_Token ON dbo.tblRefreshToken (Token);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblRefreshToken_UserId' AND object_id = OBJECT_ID(N'dbo.tblRefreshToken'))
BEGIN
    CREATE INDEX IDX_tblRefreshToken_UserId ON dbo.tblRefreshToken (UserId);
END;
GO
