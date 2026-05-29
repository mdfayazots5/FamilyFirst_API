IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_CalendarEvents_FamilyId_StartDateTime'
        AND idx.object_id = OBJECT_ID(N'dbo.CalendarEvents')
)
BEGIN
    CREATE INDEX IX_CalendarEvents_FamilyId_StartDateTime
        ON dbo.CalendarEvents
        (
            FamilyId,
            StartDateTime
        )
        WHERE IsDeleted = 0;
END;
GO
