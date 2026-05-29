IF OBJECT_ID(N'dbo.TeacherChildAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherChildAssignments
    (
        AssignmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TeacherChildAssignments PRIMARY KEY DEFAULT NEWID(),
        TeacherProfileId UNIQUEIDENTIFIER NOT NULL,
        ChildProfileId UNIQUEIDENTIFIER NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherChildAssignments_AssignedAt DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL CONSTRAINT DF_TeacherChildAssignments_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherChildAssignments_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_TeacherChildAssignments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_TeacherChildAssignments_IsDeleted DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_TeacherChildAssignments_TeacherProfiles_TeacherProfileId FOREIGN KEY (TeacherProfileId) REFERENCES dbo.TeacherProfiles (TeacherProfileId),
        CONSTRAINT FK_TeacherChildAssignments_ChildProfiles_ChildProfileId FOREIGN KEY (ChildProfileId) REFERENCES dbo.ChildProfiles (ChildProfileId),
        CONSTRAINT FK_TeacherChildAssignments_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TeacherChildAssignments_Teacher_Child'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherChildAssignments')
)
BEGIN
    CREATE UNIQUE INDEX IX_TeacherChildAssignments_Teacher_Child
        ON dbo.TeacherChildAssignments
        (
            TeacherProfileId,
            ChildProfileId
        )
        WHERE IsActive = 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TeacherChildAssignments_FamilyId'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherChildAssignments')
)
BEGIN
    CREATE INDEX IX_TeacherChildAssignments_FamilyId
        ON dbo.TeacherChildAssignments
        (
            FamilyId
        );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TeacherChildAssignments_ChildProfileId'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherChildAssignments')
)
BEGIN
    CREATE INDEX IX_TeacherChildAssignments_ChildProfileId
        ON dbo.TeacherChildAssignments
        (
            ChildProfileId
        );
END;
GO
