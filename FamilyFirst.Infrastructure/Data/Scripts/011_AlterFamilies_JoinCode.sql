IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UX_Families_JoinCode'
        AND idx.object_id = OBJECT_ID(N'dbo.Families')
)
BEGIN
    CREATE UNIQUE INDEX UX_Families_JoinCode
        ON dbo.Families
        (
            JoinCode
        )
        WHERE IsDeleted = 0;
END;
GO
