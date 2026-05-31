IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IDX_tblAttendanceSession_TeacherProfileId_ScheduledDate'
        AND idx.object_id = OBJECT_ID(N'dbo.tblAttendanceSession')
)
BEGIN
    CREATE INDEX IDX_tblAttendanceSession_TeacherProfileId_ScheduledDate
        ON dbo.tblAttendanceSession
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
    WHERE idx.name = N'IDX_tblAttendanceSession_FamilyId_ScheduledDate'
        AND idx.object_id = OBJECT_ID(N'dbo.tblAttendanceSession')
)
BEGIN
    CREATE INDEX IDX_tblAttendanceSession_FamilyId_ScheduledDate
        ON dbo.tblAttendanceSession
        (
            FamilyId,
            ScheduledDate
        )
        WHERE IsDeleted = 0
            AND IsActive = 1;
END;
GO
