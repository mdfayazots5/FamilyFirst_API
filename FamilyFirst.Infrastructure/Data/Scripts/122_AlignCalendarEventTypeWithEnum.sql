SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (R6 demo/live drift fix)
-- Date Created     : 21 Nov 2026
-- Description      : Aligns tblCalendarEventType (served by GetMasters 'CalendarEventType') with the backend
--                    EventType enum (FamilyFirst.Domain/Enums/EventType.cs) that tblCalendarEvent and the API use.
--                    093 seeded FAMILY/SCHOOL/HOLIDAY/PERSONAL/MEDICAL, which match no event type, so live filter
--                    chips and type labels never matched an event. This script soft-deletes those five rows and
--                    inserts one row per enum member (Code = enum name, SortOrder = enum value). Idempotent.
--                    No table references tblCalendarEventType rows (verified: only the GetMasters SPs read it).
-- Usage            : sqlcmd -I -b -i 122_AlignCalendarEventTypeWithEnum.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      21 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------

UPDATE cet
SET    cet.IsDeleted   = 1,
       cet.DateDeleted = GETUTCDATE(),
       cet.DeletedBy   = N'System'
FROM   dbo.tblCalendarEventType cet
WHERE  cet.IsDeleted = 0
  AND  cet.CalendarEventTypeCode IN (N'FAMILY', N'SCHOOL', N'HOLIDAY', N'PERSONAL', N'MEDICAL');
GO

INSERT INTO dbo.tblCalendarEventType
    (CalendarEventTypeName, CalendarEventTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.CalendarEventTypeName, source.CalendarEventTypeCode, source.SortOrder, 1, 1, 0, GETUTCDATE(), N'System'
FROM (VALUES
    (N'Doctor Appointment', N'DoctorAppointment', 1),
    (N'School Event',       N'SchoolEvent',       2),
    (N'Tuition Class',      N'TuitionClass',      3),
    (N'Birthday',           N'Birthday',          4),
    (N'Medicine Reminder',  N'MedicineReminder',  5),
    (N'Exam Date',          N'ExamDate',          6),
    (N'Family Travel',      N'FamilyTravel',      7),
    (N'Other',              N'Other',             8)
) AS source (CalendarEventTypeName, CalendarEventTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblCalendarEventType t
    WHERE t.CalendarEventTypeCode = source.CalendarEventTypeCode AND t.IsDeleted = 0
);
GO
