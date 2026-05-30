IF OBJECT_ID(N'dbo.SafeZones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SafeZones
    (
        SafeZoneId              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SafeZones PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        ZoneName                NVARCHAR(40)     NOT NULL,
        -- ZoneType: Home / School / Tuition / RelativesHouse / Workplace / PlaceOfWorship / Other
        ZoneType                NVARCHAR(30)     NOT NULL,
        CenterLatitude          DECIMAL(10,7)    NOT NULL,
        CenterLongitude         DECIMAL(10,7)    NOT NULL,
        RadiusMetres            INT              NOT NULL CONSTRAINT DF_SafeZones_RadiusMetres DEFAULT 150,
        AlertOnArrival          BIT              NOT NULL CONSTRAINT DF_SafeZones_AlertOnArrival   DEFAULT 1,
        AlertOnDeparture        BIT              NOT NULL CONSTRAINT DF_SafeZones_AlertOnDeparture DEFAULT 1,
        LateAlertEnabled        BIT              NOT NULL CONSTRAINT DF_SafeZones_LateAlertEnabled DEFAULT 0,
        LateAlertTime           TIME             NULL,
        OverrideQuietHours      BIT              NOT NULL CONSTRAINT DF_SafeZones_OverrideQuietHours DEFAULT 1,
        AppliedMemberIdsJson    NVARCHAR(2000)   NOT NULL,
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_SafeZones_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_SafeZones_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_SafeZones_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_SafeZones_Families_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT CK_SafeZones_ZoneName
            CHECK (LEN(ZoneName) BETWEEN 1 AND 40),
        CONSTRAINT CK_SafeZones_RadiusMetres
            CHECK (RadiusMetres BETWEEN 50 AND 500),
        CONSTRAINT CK_SafeZones_ZoneType
            CHECK (ZoneType IN (
                N'Home', N'School', N'Tuition', N'RelativesHouse',
                N'Workplace', N'PlaceOfWorship', N'Other'
            )),
        CONSTRAINT CK_SafeZones_LateAlertTime
            CHECK (LateAlertEnabled = 0 OR LateAlertTime IS NOT NULL)
    );
END;
GO

-- Row-level security — family map query
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SafeZones_FamilyId_IsDeleted'
      AND object_id = OBJECT_ID(N'dbo.SafeZones')
)
BEGIN
    CREATE INDEX IX_SafeZones_FamilyId_IsDeleted
        ON dbo.SafeZones (FamilyId, IsDeleted)
        WHERE IsDeleted = 0;
END;
GO
