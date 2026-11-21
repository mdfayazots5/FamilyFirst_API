-- ============================================================
-- Script  : 113_SeedMasterData_Level2.sql
-- Purpose : Register the 4 Level 2 MasterDataCode pointer rows in tblMasterData.
--           MasterDataCode MUST exactly equal the MasterDataCodes enum name
--           (FamilyFirst.Domain/Enums/MasterDataCodes.cs 20–23) — the API
--           calls the SPs with Enum.ToString().
--           Same pointer pattern as 083_SeedMasterData.sql: no inline values;
--           the SPs route each code to its dedicated table (114 / 115).
-- Depends : 073_CreateMasterData.sql, 111_CreateLookupTables_Level2.sql
-- Idempotent: NOT EXISTS guard on MasterDataCode — safe to re-run.
-- ============================================================

INSERT INTO dbo.tblMasterData
    (MasterDataName, MasterDataCode, IsMasterData, MasterCodeSpName,
     SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT
    source.MasterDataName, source.MasterDataCode, 0, source.MasterCodeSpName,
    source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    -- Level 2 simple lookup tables (created in 111_CreateLookupTables_Level2.sql)
    (N'tblDocumentCategory',    N'DocumentCategory',    NULL, 20),
    (N'tblHealthRecordType',    N'HealthRecordType',    NULL, 21),
    (N'tblSafeZoneType',        N'SafeZoneType',        NULL, 22),
    (N'tblFinanceCategory',     N'FinanceCategory',     NULL, 23)
) AS source (MasterDataName, MasterDataCode, MasterCodeSpName, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblMasterData t
    WHERE t.MasterDataCode = source.MasterDataCode
      AND t.IsDeleted      = 0
);
GO
