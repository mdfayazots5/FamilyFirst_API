IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IDX_tblTaskItem_FamilyId_ChildProfileId_IsActive'
        AND idx.object_id = OBJECT_ID(N'dbo.tblTaskItem')
)
BEGIN
    CREATE INDEX IDX_tblTaskItem_FamilyId_ChildProfileId_IsActive
        ON dbo.tblTaskItem
        (
            FamilyId,
            ChildProfileId,
            IsActive
        )
        WHERE IsDeleted = 0;
END;
GO
