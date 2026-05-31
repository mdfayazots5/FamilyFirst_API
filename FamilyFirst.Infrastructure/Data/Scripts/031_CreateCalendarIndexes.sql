IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IDX_tblCalendarEvent_FamilyId_StartDateTime'
        AND idx.object_id = OBJECT_ID(N'dbo.tblCalendarEvent')
)
BEGIN
    CREATE INDEX IDX_tblCalendarEvent_FamilyId_StartDateTime
        ON dbo.tblCalendarEvent
        (
            FamilyId,
            StartDateTime
        )
        WHERE IsDeleted = 0;
END;
GO
