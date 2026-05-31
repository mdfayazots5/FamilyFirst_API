IF OBJECT_ID(N'dbo.tblEmergencyCardLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmergencyCardLink
    (
        EmergencyCardLinkId     BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        HealthProfileId         BIGINT NOT NULL,
        FamilyId                BIGINT NOT NULL,
        CreatedByUserId         BIGINT NOT NULL,
        Token                   NVARCHAR(256) NOT NULL,
        Language                NVARCHAR(16) NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_Language DEFAULT (N'en'),
        ExpiresAt               DATETIME2 NOT NULL,
        IsRevoked               BIT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_IsRevoked DEFAULT (0),
        RevokedAt               DATETIME2 NULL,
        LastAccessedAt          DATETIME2 NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblEmergencyCardLink_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblEmergencyCardLink_EmergencyCardLinkId PRIMARY KEY (EmergencyCardLinkId),
        CONSTRAINT FK_tblEmergencyCardLink_HealthProfileId_tblHealthProfile_HealthProfileId
            FOREIGN KEY (HealthProfileId) REFERENCES dbo.tblHealthProfile (HealthProfileId),
        CONSTRAINT FK_tblEmergencyCardLink_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblEmergencyCardLink_CreatedByUserId_tblUser_UserId
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.tblUser (UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblEmergencyCardLink_Id' AND object_id = OBJECT_ID(N'dbo.tblEmergencyCardLink'))
BEGIN
    CREATE UNIQUE INDEX UK_tblEmergencyCardLink_Id ON dbo.tblEmergencyCardLink (Id) WHERE IsDeleted = 0;
END;
GO

-- Token lookup — every unauthenticated emergency card request resolves by Token
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblEmergencyCardLink_Token' AND object_id = OBJECT_ID(N'dbo.tblEmergencyCardLink'))
BEGIN
    CREATE UNIQUE INDEX UK_tblEmergencyCardLink_Token
        ON dbo.tblEmergencyCardLink (Token)
        WHERE IsDeleted = 0;
END;
GO

-- Active links per health profile — list and revoke from MR-05
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblEmergencyCardLink_HealthProfileId_IsRevoked' AND object_id = OBJECT_ID(N'dbo.tblEmergencyCardLink'))
BEGIN
    CREATE INDEX IDX_tblEmergencyCardLink_HealthProfileId_IsRevoked
        ON dbo.tblEmergencyCardLink (HealthProfileId, IsRevoked)
        WHERE IsDeleted = 0;
END;
GO
