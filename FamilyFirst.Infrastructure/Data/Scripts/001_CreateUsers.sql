IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY DEFAULT NEWID(),
        PhoneNumber NVARCHAR(20) NOT NULL,
        CountryCode NVARCHAR(5) NOT NULL CONSTRAINT DF_Users_CountryCode DEFAULT N'+91',
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(300) NULL,
        ProfilePhotoUrl NVARCHAR(500) NULL,
        PinHash NVARCHAR(500) NULL,
        PasswordHash NVARCHAR(500) NULL,
        FcmToken NVARCHAR(500) NULL,
        IsPhoneVerified BIT NOT NULL CONSTRAINT DF_Users_IsPhoneVerified DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        PreferredLanguage NVARCHAR(10) NOT NULL CONSTRAINT DF_Users_PreferredLanguage DEFAULT N'en',
        LastLoginAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_PhoneNumber' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_PhoneNumber ON dbo.Users (PhoneNumber) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email) WHERE Email IS NOT NULL AND IsDeleted = 0;
END;
GO
