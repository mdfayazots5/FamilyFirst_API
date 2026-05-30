IF OBJECT_ID(N'dbo.EmergencyCardLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmergencyCardLinks
    (
        EmergencyCardLinkId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmergencyCardLinks PRIMARY KEY DEFAULT NEWID(),
        HealthProfileId     UNIQUEIDENTIFIER NOT NULL,
        FamilyId            UNIQUEIDENTIFIER NOT NULL,
        CreatedByUserId     UNIQUEIDENTIFIER NOT NULL,
        Token               NVARCHAR(200)    NOT NULL,
        Language            NVARCHAR(10)     NOT NULL CONSTRAINT DF_EmergencyCardLinks_Language DEFAULT N'en',
        ExpiresAt           DATETIME2        NOT NULL,
        IsRevoked           BIT              NOT NULL CONSTRAINT DF_EmergencyCardLinks_IsRevoked     DEFAULT 0,
        RevokedAt           DATETIME2        NULL,
        LastAccessedAt      DATETIME2        NULL,
        CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_EmergencyCardLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2        NOT NULL CONSTRAINT DF_EmergencyCardLinks_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted           BIT              NOT NULL CONSTRAINT DF_EmergencyCardLinks_IsDeleted  DEFAULT 0,
        DeletedAt           DATETIME2        NULL,

        CONSTRAINT FK_EmergencyCardLinks_HealthProfiles_HealthProfileId
            FOREIGN KEY (HealthProfileId)  REFERENCES dbo.HealthProfiles (HealthProfileId),
        CONSTRAINT FK_EmergencyCardLinks_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families       (FamilyId),
        CONSTRAINT FK_EmergencyCardLinks_Users_CreatedByUserId
            FOREIGN KEY (CreatedByUserId)  REFERENCES dbo.Users          (UserId)
    );
END;
GO

-- Token lookup — every unauthenticated emergency card request resolves by Token
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_EmergencyCardLinks_Token'
      AND object_id = OBJECT_ID(N'dbo.EmergencyCardLinks')
)
BEGIN
    CREATE UNIQUE INDEX UX_EmergencyCardLinks_Token
        ON dbo.EmergencyCardLinks (Token)
        WHERE IsDeleted = 0;
END;
GO

-- Active links per health profile — list and revoke from MR-05
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EmergencyCardLinks_HealthProfileId_IsRevoked'
      AND object_id = OBJECT_ID(N'dbo.EmergencyCardLinks')
)
BEGIN
    CREATE INDEX IX_EmergencyCardLinks_HealthProfileId_IsRevoked
        ON dbo.EmergencyCardLinks (HealthProfileId, IsRevoked)
        WHERE IsDeleted = 0;
END;
GO
