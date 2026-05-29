IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TeacherFeedback_FamilyId_ChildProfileId_FeedbackType'
        AND idx.object_id = OBJECT_ID(N'dbo.TeacherFeedback')
)
BEGIN
    CREATE INDEX IX_TeacherFeedback_FamilyId_ChildProfileId_FeedbackType
        ON dbo.TeacherFeedback
        (
            FamilyId,
            ChildProfileId,
            FeedbackType
        )
        WHERE IsDeleted = 0;
END;
GO
