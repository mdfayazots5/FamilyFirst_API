IF OBJECT_ID(N'dbo.HealthProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HealthProfiles
    (
        HealthProfileId           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HealthProfiles PRIMARY KEY DEFAULT NEWID(),
        FamilyId                  UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId            UNIQUEIDENTIFIER NOT NULL,
        BloodGroup                NVARCHAR(10)     NOT NULL CONSTRAINT DF_HealthProfiles_BloodGroup DEFAULT N'',
        KnownAllergiesJson        NVARCHAR(4000)   NULL,
        ChronicConditionsJson     NVARCHAR(2000)   NULL,
        PrimaryDoctorName         NVARCHAR(200)    NULL,
        PrimaryDoctorPhone        NVARCHAR(20)     NULL,
        EmergencyContactName      NVARCHAR(200)    NULL,
        EmergencyContactRelationship NVARCHAR(100) NULL,
        EmergencyContactPhone     NVARCHAR(20)     NULL,
        OrganDonor                BIT              NOT NULL CONSTRAINT DF_HealthProfiles_OrganDonor DEFAULT 0,
        CreatedAt                 DATETIME2        NOT NULL CONSTRAINT DF_HealthProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt                 DATETIME2        NOT NULL CONSTRAINT DF_HealthProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted                 BIT              NOT NULL CONSTRAINT DF_HealthProfiles_IsDeleted  DEFAULT 0,
        DeletedAt                 DATETIME2        NULL,

        CONSTRAINT FK_HealthProfiles_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_HealthProfiles_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT CK_HealthProfiles_BloodGroup
            CHECK (BloodGroup IN (N'', N'A+', N'A-', N'B+', N'B-', N'AB+', N'AB-', N'O+', N'O-'))
    );
END;
GO

-- One health profile per member — enforced at DB level
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_HealthProfiles_FamilyMemberId'
      AND object_id = OBJECT_ID(N'dbo.HealthProfiles')
)
BEGIN
    CREATE UNIQUE INDEX UX_HealthProfiles_FamilyMemberId
        ON dbo.HealthProfiles (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Row-level security — every query filters by FamilyId
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HealthProfiles_FamilyId_IsDeleted'
      AND object_id = OBJECT_ID(N'dbo.HealthProfiles')
)
BEGIN
    CREATE INDEX IX_HealthProfiles_FamilyId_IsDeleted
        ON dbo.HealthProfiles (FamilyId, IsDeleted)
        WHERE IsDeleted = 0;
END;
GO
