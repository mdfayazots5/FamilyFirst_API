-- Unique composite index ensuring one active membership record per user per family
-- Note: UK_tblFamilyMember_FamilyId_UserId is also created inline in 006_CreateFamilyMembers.sql;
-- this script is idempotent and safe to run after 006.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UK_tblFamilyMember_FamilyId_UserId'
        AND idx.object_id = OBJECT_ID(N'dbo.tblFamilyMember')
)
BEGIN
    CREATE UNIQUE INDEX UK_tblFamilyMember_FamilyId_UserId
        ON dbo.tblFamilyMember
        (
            FamilyId,
            UserId
        )
        WHERE IsDeleted = 0;
END;
GO
