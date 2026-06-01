-- ============================================================
-- Script  : 093_SeedLookupTables.sql
-- Purpose : Seed all FamilyFirst simple lookup tables created
--           in 092_CreateLookupTables.sql.
--           Each value has a system-generated GUID (Id) via DEFAULT.
--           These GUIDs are what the UI receives and sends back.
-- Depends : 092_CreateLookupTables.sql
-- ============================================================

-- ── tblTaskType ───────────────────────────────────────────────────────────
INSERT INTO dbo.tblTaskType (TaskTypeName, TaskTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.TaskTypeName, source.TaskTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Academic',   N'ACADEMIC',   1),
    (N'Physical',   N'PHYSICAL',   2),
    (N'Household',  N'HOUSEHOLD',  3),
    (N'Creative',   N'CREATIVE',   4),
    (N'Social',     N'SOCIAL',     5)
) AS source (TaskTypeName, TaskTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblTaskType t
    WHERE t.TaskTypeCode = source.TaskTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblTaskStatus ─────────────────────────────────────────────────────────
INSERT INTO dbo.tblTaskStatus (TaskStatusName, TaskStatusCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.TaskStatusName, source.TaskStatusCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Pending',    N'PENDING',     1),
    (N'InProgress', N'INPROGRESS',  2),
    (N'Completed',  N'COMPLETED',   3),
    (N'Approved',   N'APPROVED',    4),
    (N'Rejected',   N'REJECTED',    5)
) AS source (TaskStatusName, TaskStatusCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblTaskStatus t
    WHERE t.TaskStatusCode = source.TaskStatusCode AND t.IsDeleted = 0
);
GO

-- ── tblAttendanceStatus ───────────────────────────────────────────────────
INSERT INTO dbo.tblAttendanceStatus (AttendanceStatusName, AttendanceStatusCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.AttendanceStatusName, source.AttendanceStatusCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Present',    N'PRESENT',     1),
    (N'Absent',     N'ABSENT',      2),
    (N'Late',       N'LATE',        3),
    (N'HalfDay',    N'HALFDAY',     4)
) AS source (AttendanceStatusName, AttendanceStatusCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblAttendanceStatus t
    WHERE t.AttendanceStatusCode = source.AttendanceStatusCode AND t.IsDeleted = 0
);
GO

-- ── tblRewardType ─────────────────────────────────────────────────────────
INSERT INTO dbo.tblRewardType (RewardTypeName, RewardTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.RewardTypeName, source.RewardTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Digital',    N'DIGITAL',     1),
    (N'Physical',   N'PHYSICAL',    2),
    (N'Experience', N'EXPERIENCE',  3)
) AS source (RewardTypeName, RewardTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblRewardType t
    WHERE t.RewardTypeCode = source.RewardTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblCoinTransactionType ────────────────────────────────────────────────
INSERT INTO dbo.tblCoinTransactionType (CoinTransactionTypeName, CoinTransactionTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.CoinTransactionTypeName, source.CoinTransactionTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Earn',   N'EARN',    1),
    (N'Spend',  N'SPEND',   2),
    (N'Bonus',  N'BONUS',   3),
    (N'Deduct', N'DEDUCT',  4)
) AS source (CoinTransactionTypeName, CoinTransactionTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblCoinTransactionType t
    WHERE t.CoinTransactionTypeCode = source.CoinTransactionTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblFeedbackRating ─────────────────────────────────────────────────────
INSERT INTO dbo.tblFeedbackRating (FeedbackRatingName, FeedbackRatingCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.FeedbackRatingName, source.FeedbackRatingCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Excellent',          N'EXCELLENT',   1),
    (N'Good',               N'GOOD',        2),
    (N'Satisfactory',       N'SATISFACTORY',3),
    (N'NeedsImprovement',   N'NEEDSIMPROVE',4)
) AS source (FeedbackRatingName, FeedbackRatingCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblFeedbackRating t
    WHERE t.FeedbackRatingCode = source.FeedbackRatingCode AND t.IsDeleted = 0
);
GO

-- ── tblCalendarEventType ──────────────────────────────────────────────────
INSERT INTO dbo.tblCalendarEventType (CalendarEventTypeName, CalendarEventTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.CalendarEventTypeName, source.CalendarEventTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Family',     N'FAMILY',      1),
    (N'School',     N'SCHOOL',      2),
    (N'Holiday',    N'HOLIDAY',     3),
    (N'Personal',   N'PERSONAL',    4),
    (N'Medical',    N'MEDICAL',     5)
) AS source (CalendarEventTypeName, CalendarEventTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblCalendarEventType t
    WHERE t.CalendarEventTypeCode = source.CalendarEventTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblNotificationType ───────────────────────────────────────────────────
INSERT INTO dbo.tblNotificationType (NotificationTypeName, NotificationTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.NotificationTypeName, source.NotificationTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Attendance', N'ATTENDANCE',  1),
    (N'Task',       N'TASK',        2),
    (N'Reward',     N'REWARD',      3),
    (N'Feedback',   N'FEEDBACK',    4),
    (N'Calendar',   N'CALENDAR',    5),
    (N'System',     N'SYSTEM',      6)
) AS source (NotificationTypeName, NotificationTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblNotificationType t
    WHERE t.NotificationTypeCode = source.NotificationTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblOTPType ────────────────────────────────────────────────────────────
INSERT INTO dbo.tblOTPType (OTPTypeName, OTPTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.OTPTypeName, source.OTPTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Login',  N'LOGIN',   1),
    (N'SetPIN', N'SETPIN',  2)
) AS source (OTPTypeName, OTPTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblOTPType t
    WHERE t.OTPTypeCode = source.OTPTypeCode AND t.IsDeleted = 0
);
GO
