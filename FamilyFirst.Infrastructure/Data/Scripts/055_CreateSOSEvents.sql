IF OBJECT_ID(N'dbo.SOSEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SOSEvents
    (
        SOSEventId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SOSEvents PRIMARY KEY DEFAULT NEWID(),
        FamilyId            UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId      UNIQUEIDENTIFIER NOT NULL,
        LocationAlertId     UNIQUEIDENTIFIER NOT NULL,
        Latitude            DECIMAL(10,7)    NOT NULL,
        Longitude           DECIMAL(10,7)    NOT NULL,
        DispatchedAt        DATETIME2        NOT NULL,
        AlertsSentCount     INT              NOT NULL CONSTRAINT DF_SOSEvents_AlertsSentCount DEFAULT 0,
        ResolvedAt          DATETIME2        NULL,
        ResolvedByUserId    UNIQUEIDENTIFIER NULL,
        CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_SOSEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2        NOT NULL CONSTRAINT DF_SOSEvents_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted           BIT              NOT NULL CONSTRAINT DF_SOSEvents_IsDeleted  DEFAULT 0,
        DeletedAt           DATETIME2        NULL,

        CONSTRAINT FK_SOSEvents_Families_FamilyId
            FOREIGN KEY (FamilyId)         REFERENCES dbo.Families        (FamilyId),
        CONSTRAINT FK_SOSEvents_ChildProfiles_ChildProfileId
            FOREIGN KEY (ChildProfileId)   REFERENCES dbo.ChildProfiles   (ChildProfileId),
        CONSTRAINT FK_SOSEvents_LocationAlerts_LocationAlertId
            FOREIGN KEY (LocationAlertId)  REFERENCES dbo.LocationAlerts  (LocationAlertId),
        CONSTRAINT FK_SOSEvents_Users_ResolvedByUserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users           (UserId)
    );
END;
GO

-- Active (unresolved) SOS events per family — parent map SOS indicator
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SOSEvents_FamilyId_ResolvedAt'
      AND object_id = OBJECT_ID(N'dbo.SOSEvents')
)
BEGIN
    CREATE INDEX IX_SOSEvents_FamilyId_ResolvedAt
        ON dbo.SOSEvents (FamilyId, ResolvedAt)
        WHERE IsDeleted = 0 AND ResolvedAt IS NULL;
END;
GO
