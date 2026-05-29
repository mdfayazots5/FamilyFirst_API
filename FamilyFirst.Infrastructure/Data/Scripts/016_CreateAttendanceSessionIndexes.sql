IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AttendanceSessions_TeacherProfileId_ScheduledDate'
        AND idx.object_id = OBJECT_ID(N'dbo.AttendanceSessions')
)
BEGIN
    CREATE INDEX IX_AttendanceSessions_TeacherProfileId_ScheduledDate
        ON dbo.AttendanceSessions
        (
            TeacherProfileId,
            ScheduledDate
        )
        WHERE IsDeleted = 0
            AND IsActive = 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_AttendanceSessions_FamilyId_ScheduledDate'
        AND idx.object_id = OBJECT_ID(N'dbo.AttendanceSessions')
)
BEGIN
    CREATE INDEX IX_AttendanceSessions_FamilyId_ScheduledDate
        ON dbo.AttendanceSessions
        (
            FamilyId,
            ScheduledDate
        )
        WHERE IsDeleted = 0
            AND IsActive = 1;
END;
GO
