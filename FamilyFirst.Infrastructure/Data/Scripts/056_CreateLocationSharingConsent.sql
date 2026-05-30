IF OBJECT_ID(N'dbo.LocationSharingConsent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocationSharingConsent
    (
        ConsentId           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LocationSharingConsent PRIMARY KEY DEFAULT NEWID(),
        FamilyId            UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId      UNIQUEIDENTIFIER NOT NULL,
        ConsentGiven        BIT              NOT NULL CONSTRAINT DF_LocationSharingConsent_ConsentGiven    DEFAULT 0,
        SharingEnabled      BIT              NOT NULL CONSTRAINT DF_LocationSharingConsent_SharingEnabled  DEFAULT 0,
        CaregiverViewOnly   BIT              NOT NULL CONSTRAINT DF_LocationSharingConsent_CaregiverViewOnly DEFAULT 0,
        ConsentGivenAt      DATETIME2        NULL,
        ConsentRevokedAt    DATETIME2        NULL,
        CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_LocationSharingConsent_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2        NOT NULL CONSTRAINT DF_LocationSharingConsent_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted           BIT              NOT NULL CONSTRAINT DF_LocationSharingConsent_IsDeleted  DEFAULT 0,
        DeletedAt           DATETIME2        NULL,

        CONSTRAINT FK_LocationSharingConsent_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_LocationSharingConsent_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId)
    );
END;
GO

-- One consent record per member — enforced at DB level
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_LocationSharingConsent_FamilyMemberId'
      AND object_id = OBJECT_ID(N'dbo.LocationSharingConsent')
)
BEGIN
    CREATE UNIQUE INDEX UX_LocationSharingConsent_FamilyMemberId
        ON dbo.LocationSharingConsent (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Settings screen — load all consent records for family
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LocationSharingConsent_FamilyId'
      AND object_id = OBJECT_ID(N'dbo.LocationSharingConsent')
)
BEGIN
    CREATE INDEX IX_LocationSharingConsent_FamilyId
        ON dbo.LocationSharingConsent (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
