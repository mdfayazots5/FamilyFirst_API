IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_TaskItems_FamilyId_ChildProfileId_IsActive'
        AND idx.object_id = OBJECT_ID(N'dbo.TaskItems')
)
BEGIN
    CREATE INDEX IX_TaskItems_FamilyId_ChildProfileId_IsActive
        ON dbo.TaskItems
        (
            FamilyId,
            ChildProfileId,
            IsActive
        )
        WHERE IsDeleted = 0;
END;
GO
