IF OBJECT_ID(N'dbo.CustomAttendanceStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomAttendanceStatuses
    (
        StatusId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CustomAttendanceStatuses PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        StatusName NVARCHAR(50) NOT NULL,
        ColorHex NVARCHAR(7) NOT NULL,
        SortOrder INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CustomAttendanceStatuses_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CustomAttendanceStatuses_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CustomAttendanceStatuses_FamilyId_StatusName' AND object_id = OBJECT_ID(N'dbo.CustomAttendanceStatuses'))
BEGIN
    CREATE UNIQUE INDEX UX_CustomAttendanceStatuses_FamilyId_StatusName
        ON dbo.CustomAttendanceStatuses (FamilyId, StatusName);
END;
GO
