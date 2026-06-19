IF OBJECT_ID(N'dbo.tblHealthProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblHealthProfile
    (
        HealthProfileId                 BIGINT IDENTITY(1,1) NOT NULL,
        Id                              UNIQUEIDENTIFIER NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_Id DEFAULT (NEWID()),
        CompanyId                       INT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_CompanyId DEFAULT (1),
        SiteId                          INT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_SiteId DEFAULT (1),
        DepartmentId                    INT NULL,

        -- Business columns
        FamilyId                        BIGINT NOT NULL,
        FamilyMemberId                  BIGINT NOT NULL,
        BloodGroup                      NVARCHAR(16) NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_BloodGroup DEFAULT (N''),
        KnownAllergiesJson              NVARCHAR(4000) NULL,
        ChronicConditionsJson           NVARCHAR(2000) NULL,
        PrimaryDoctorName               NVARCHAR(256) NULL,
        PrimaryDoctorPhone              NVARCHAR(24) NULL,
        EmergencyContactName            NVARCHAR(256) NULL,
        EmergencyContactRelationship    NVARCHAR(128) NULL,
        EmergencyContactPhone           NVARCHAR(24) NULL,
        OrganDonor                      BIT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_OrganDonor DEFAULT (0),

        -- Audit columns
        Tag                             NVARCHAR(64) NULL,
        Comments                        NVARCHAR(256) NULL,
        DisplayOnWeb                    BIT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_DisplayOnWeb DEFAULT (1),
        IsPublished                     BIT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_IsPublished DEFAULT (1),
        DatePublished                   DATETIME2 NULL,
        PublishedBy                     NVARCHAR(128) NULL,
        SortOrder                       INT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_SortOrder DEFAULT (0),
        IPAddress                       NVARCHAR(64) NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                       NVARCHAR(128) NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_CreatedBy DEFAULT (N'Admin'),
        DateCreated                     DATETIME2 NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                       NVARCHAR(128) NULL,
        LastUpdated                     DATETIME2 NULL,
        DeletedBy                       NVARCHAR(128) NULL,
        DateDeleted                     DATETIME2 NULL,
        IsDeleted                       BIT NOT NULL
                                            CONSTRAINT DF_tblHealthProfile_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblHealthProfile_HealthProfileId PRIMARY KEY (HealthProfileId),
        CONSTRAINT FK_tblHealthProfile_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblHealthProfile_FamilyMemberId_tblFamilyMember_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.tblFamilyMember (FamilyMemberId),
        CONSTRAINT CK_tblHealthProfile_BloodGroup
            CHECK (BloodGroup IN (N'', N'A+', N'A-', N'B+', N'B-', N'AB+', N'AB-', N'O+', N'O-'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblHealthProfile_Id' AND object_id = OBJECT_ID(N'dbo.tblHealthProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblHealthProfile_Id ON dbo.tblHealthProfile (Id) WHERE IsDeleted = 0;
END;
GO

-- One health profile per member — enforced at DB level
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblHealthProfile_FamilyMemberId' AND object_id = OBJECT_ID(N'dbo.tblHealthProfile'))
BEGIN
    CREATE UNIQUE INDEX UK_tblHealthProfile_FamilyMemberId
        ON dbo.tblHealthProfile (FamilyMemberId)
        WHERE IsDeleted = 0;
END;
GO

-- Row-level security — every query filters by FamilyId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblHealthProfile_FamilyId' AND object_id = OBJECT_ID(N'dbo.tblHealthProfile'))
BEGIN
    CREATE INDEX IDX_tblHealthProfile_FamilyId
        ON dbo.tblHealthProfile (FamilyId)
        WHERE IsDeleted = 0;
END;
GO
