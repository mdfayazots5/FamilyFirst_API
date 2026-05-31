IF OBJECT_ID(N'dbo.tblUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblUser
    (
        UserId              BIGINT IDENTITY(1,1) NOT NULL,
        Id                  UNIQUEIDENTIFIER NOT NULL
                                CONSTRAINT DF_tblUser_Id DEFAULT (NEWID()),
        CompanyId           INT NOT NULL
                                CONSTRAINT DF_tblUser_CompanyId DEFAULT (1),
        SiteId              INT NOT NULL
                                CONSTRAINT DF_tblUser_SiteId DEFAULT (1),
        DepartmentId        INT NULL,

        -- Business columns
        PhoneNumber         NVARCHAR(24) NOT NULL,
        CountryCode         NVARCHAR(8) NOT NULL
                                CONSTRAINT DF_tblUser_CountryCode DEFAULT (N'+91'),
        FullName            NVARCHAR(256) NOT NULL,
        Email               NVARCHAR(512) NULL,
        ProfilePhotoUrl     NVARCHAR(512) NULL,
        PinHash             NVARCHAR(512) NULL,
        PasswordHash        NVARCHAR(512) NULL,
        FcmToken            NVARCHAR(512) NULL,
        IsPhoneVerified     BIT NOT NULL
                                CONSTRAINT DF_tblUser_IsPhoneVerified DEFAULT (0),
        IsActive            BIT NOT NULL
                                CONSTRAINT DF_tblUser_IsActive DEFAULT (1),
        PreferredLanguage   NVARCHAR(16) NOT NULL
                                CONSTRAINT DF_tblUser_PreferredLanguage DEFAULT (N'en'),
        LastLoginAt         DATETIME2 NULL,

        -- Audit columns
        Tag                 NVARCHAR(64) NULL,
        Comments            NVARCHAR(256) NULL,
        DisplayOnWeb        BIT NOT NULL
                                CONSTRAINT DF_tblUser_DisplayOnWeb DEFAULT (1),
        IsPublished         BIT NOT NULL
                                CONSTRAINT DF_tblUser_IsPublished DEFAULT (1),
        DatePublished       DATETIME2 NULL,
        PublishedBy         NVARCHAR(128) NULL,
        SortOrder           INT NOT NULL
                                CONSTRAINT DF_tblUser_SortOrder DEFAULT (0),
        IPAddress           NVARCHAR(64) NOT NULL
                                CONSTRAINT DF_tblUser_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy           NVARCHAR(128) NOT NULL
                                CONSTRAINT DF_tblUser_CreatedBy DEFAULT (N'Admin'),
        DateCreated         DATETIME2 NOT NULL
                                CONSTRAINT DF_tblUser_DateCreated DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(128) NULL,
        LastUpdated         DATETIME2 NULL,
        DeletedBy           NVARCHAR(128) NULL,
        DateDeleted         DATETIME2 NULL,
        IsDeleted           BIT NOT NULL
                                CONSTRAINT DF_tblUser_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblUser_UserId PRIMARY KEY (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblUser_Id' AND object_id = OBJECT_ID(N'dbo.tblUser'))
BEGIN
    CREATE UNIQUE INDEX UK_tblUser_Id ON dbo.tblUser (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblUser_PhoneNumber' AND object_id = OBJECT_ID(N'dbo.tblUser'))
BEGIN
    CREATE UNIQUE INDEX UK_tblUser_PhoneNumber ON dbo.tblUser (PhoneNumber) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblUser_Email' AND object_id = OBJECT_ID(N'dbo.tblUser'))
BEGIN
    CREATE UNIQUE INDEX UK_tblUser_Email ON dbo.tblUser (Email) WHERE Email IS NOT NULL AND IsDeleted = 0;
END;
GO
