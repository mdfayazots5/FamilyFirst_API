-- Unique partial index on JoinCode for active families.
-- Note: UK_tblFamily_JoinCode is also created inline in 004_CreateFamilies.sql;
-- this script is idempotent and safe to run after 004.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'UK_tblFamily_JoinCode'
        AND idx.object_id = OBJECT_ID(N'dbo.tblFamily')
)
BEGIN
    CREATE UNIQUE INDEX UK_tblFamily_JoinCode
        ON dbo.tblFamily (JoinCode)
        WHERE IsDeleted = 0;
END;
GO
