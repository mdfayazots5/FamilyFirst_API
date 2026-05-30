IF OBJECT_ID(N'dbo.LocationAlerts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocationAlerts
    (
        LocationAlertId     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LocationAlerts PRIMARY KEY DEFAULT NEWID(),
        FamilyId            UNIQUEIDENTIFIER NOT NULL,
        FamilyMemberId      UNIQUEIDENTIFIER NOT NULL,
        -- AlertType: ZoneArrival / ZoneDeparture / LateAlert / SOS / BatteryWarning / LocationStale / LocationSharingPaused
        AlertType           NVARCHAR(30)     NOT NULL,
        -- ZoneId nullable — zone may be soft-deleted; name preserved in ZoneNameSnapshot
        ZoneId              UNIQUEIDENTIFIER NULL,
        ZoneNameSnapshot    NVARCHAR(40)     NULL,
        Latitude            DECIMAL(10,7)    NULL,
        Longitude           DECIMAL(10,7)    NULL,
        IsResolved          BIT              NOT NULL CONSTRAINT DF_LocationAlerts_IsResolved DEFAULT 0,
        ResolvedAt          DATETIME2        NULL,
        ResolvedByUserId    UNIQUEIDENTIFIER NULL,
        ResolutionNote      NVARCHAR(500)    NULL,
        TriggeredAt         DATETIME2        NOT NULL,
        CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_LocationAlerts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2        NOT NULL CONSTRAINT DF_LocationAlerts_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted           BIT              NOT NULL CONSTRAINT DF_LocationAlerts_IsDeleted  DEFAULT 0,
        DeletedAt           DATETIME2        NULL,

        CONSTRAINT FK_LocationAlerts_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families      (FamilyId),
        CONSTRAINT FK_LocationAlerts_FamilyMembers_FamilyMemberId
            FOREIGN KEY (FamilyMemberId) REFERENCES dbo.FamilyMembers (FamilyMemberId),
        CONSTRAINT FK_LocationAlerts_SafeZones_ZoneId
            FOREIGN KEY (ZoneId)         REFERENCES dbo.SafeZones     (SafeZoneId),
        CONSTRAINT FK_LocationAlerts_Users_ResolvedByUserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users       (UserId),
        CONSTRAINT CK_LocationAlerts_AlertType
            CHECK (AlertType IN (
                N'ZoneArrival', N'ZoneDeparture', N'LateAlert', N'SOS',
                N'BatteryWarning', N'LocationStale', N'LocationSharingPaused'
            ))
    );
END;
GO

-- Alert history list — newest first, family scoped
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LocationAlerts_FamilyId_TriggeredAt'
      AND object_id = OBJECT_ID(N'dbo.LocationAlerts')
)
BEGIN
    CREATE INDEX IX_LocationAlerts_FamilyId_TriggeredAt
        ON dbo.LocationAlerts (FamilyId, TriggeredAt DESC)
        WHERE IsDeleted = 0;
END;
GO

-- SOS indicator — active unresolved SOS per member on parent map
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LocationAlerts_FamilyMemberId_AlertType'
      AND object_id = OBJECT_ID(N'dbo.LocationAlerts')
)
BEGIN
    CREATE INDEX IX_LocationAlerts_FamilyMemberId_AlertType
        ON dbo.LocationAlerts (FamilyMemberId, AlertType)
        WHERE IsDeleted = 0 AND IsResolved = 0;
END;
GO
