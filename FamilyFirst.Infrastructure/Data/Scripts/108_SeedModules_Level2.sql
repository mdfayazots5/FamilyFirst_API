-- ============================================================
-- Script  : 108_SeedModules_Level2.sql
-- Purpose : Seed tblModule with FamilyFirst's 5 Level 2 modules.
--           ModuleId values match FamilyFirstModule enum values 11–15
--           (FamilyFirst.Domain/Enums/FamilyFirstModule.cs).
--           Advanced Admin Configuration (L2) is NOT a separate module —
--           it is governed by ADMIN (ModuleId 10).
-- Depends : 069_CreateModule.sql, 080_SeedModules.sql
-- Idempotent: MERGE on ModuleId — safe to re-run.
-- ============================================================

SET IDENTITY_INSERT dbo.tblModule ON;
GO

MERGE dbo.tblModule AS target
USING (VALUES
    (11, N'Document Vault',     N'VAULT',    NULL, N'Family document storage, expiry tracking, emergency folder, secure share links.',          11),
    (12, N'Medical Records',    N'MEDICAL',  NULL, N'Health profiles, prescriptions, vaccinations, health timeline, emergency card.',            12),
    (13, N'Safety & Location',  N'SAFETY',   NULL, N'Safe zones, location sharing consent, location alerts, SOS events.',                       13),
    (14, N'Family Finance',     N'FINANCE',  NULL, N'SMS ledger transactions, budgets, commitments, consent and privacy tiers.',                14),
    (15, N'Reports & Insights', N'REPORTS',  NULL, N'Weekly digest, monthly report, attendance summary, pillar scores, report archive.',        15)
) AS source (ModuleId, ModuleName, ModuleCode, ParentModuleId, Comments, SortOrder)
ON target.ModuleId = source.ModuleId
WHEN NOT MATCHED THEN
    INSERT (ModuleId, ModuleName, ModuleCode, ParentModuleId, Comments, SortOrder,
            IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
    VALUES (source.ModuleId, source.ModuleName, source.ModuleCode,
            source.ParentModuleId, source.Comments, source.SortOrder,
            1, 1, 0, GETDATE(), N'System');
GO

SET IDENTITY_INSERT dbo.tblModule OFF;
GO
