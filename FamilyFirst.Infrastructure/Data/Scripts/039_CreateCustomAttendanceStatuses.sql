IF OBJECT_ID(N'dbo.tblCustomAttendanceStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCustomAttendanceStatus
    (
        CustomAttendanceStatusId    BIGINT IDENTITY(1,1) NOT NULL,
        Id                          UNIQUEIDENTIFIER NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_Id DEFAULT (NEWID()),
        CompanyId                   INT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_CompanyId DEFAULT (1),
        SiteId                      INT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_SiteId DEFAULT (1),
        DepartmentId                INT NULL,

        -- Business columns
        FamilyId                    BIGINT NOT NULL,
        StatusName                  NVARCHAR(64) NOT NULL,
        ColorHex                    NVARCHAR(8) NOT NULL,

        -- Audit columns
        Tag                         NVARCHAR(64) NULL,
        Comments                    NVARCHAR(256) NULL,
        DisplayOnWeb                BIT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_DisplayOnWeb DEFAULT (1),
        IsPublished                 BIT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_IsPublished DEFAULT (1),
        DatePublished               DATETIME2 NULL,
        PublishedBy                 NVARCHAR(128) NULL,
        SortOrder                   INT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_SortOrder DEFAULT (0),
        IPAddress                   NVARCHAR(64) NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy                   NVARCHAR(128) NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_CreatedBy DEFAULT (N'Admin'),
        DateCreated                 DATETIME2 NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_DateCreated DEFAULT (GETDATE()),
        UpdatedBy                   NVARCHAR(128) NULL,
        LastUpdated                 DATETIME2 NULL,
        DeletedBy                   NVARCHAR(128) NULL,
        DateDeleted                 DATETIME2 NULL,
        IsDeleted                   BIT NOT NULL
                                        CONSTRAINT DF_tblCustomAttendanceStatus_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblCustomAttendanceStatus_CustomAttendanceStatusId
            PRIMARY KEY (CustomAttendanceStatusId),
        CONSTRAINT FK_tblCustomAttendanceStatus_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCustomAttendanceStatus_Id' AND object_id = OBJECT_ID(N'dbo.tblCustomAttendanceStatus'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCustomAttendanceStatus_Id
        ON dbo.tblCustomAttendanceStatus (Id) WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblCustomAttendanceStatus_FamilyId_StatusName' AND object_id = OBJECT_ID(N'dbo.tblCustomAttendanceStatus'))
BEGIN
    CREATE UNIQUE INDEX UK_tblCustomAttendanceStatus_FamilyId_StatusName
        ON dbo.tblCustomAttendanceStatus (FamilyId, StatusName);
END;
GO
