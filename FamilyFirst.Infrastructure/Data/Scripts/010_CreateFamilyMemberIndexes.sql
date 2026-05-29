IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_FamilyMembers_FamilyId_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.FamilyMembers')
)
BEGIN
    CREATE UNIQUE INDEX IX_FamilyMembers_FamilyId_UserId
        ON dbo.FamilyMembers
        (
            FamilyId,
            UserId
        )
        WHERE IsDeleted = 0;
END;
GO
