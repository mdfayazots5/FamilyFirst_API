IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IDX_tblTeacherFeedback_FamilyId_ChildProfileId_FeedbackType'
        AND idx.object_id = OBJECT_ID(N'dbo.tblTeacherFeedback')
)
BEGIN
    CREATE INDEX IDX_tblTeacherFeedback_FamilyId_ChildProfileId_FeedbackType
        ON dbo.tblTeacherFeedback
        (
            FamilyId,
            ChildProfileId,
            FeedbackType
        )
        WHERE IsDeleted = 0;
END;
GO
