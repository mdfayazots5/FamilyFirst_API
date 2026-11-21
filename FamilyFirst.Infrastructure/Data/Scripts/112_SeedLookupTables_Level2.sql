-- ============================================================
-- Script  : 112_SeedLookupTables_Level2.sql
-- Purpose : Seed the 4 Level 2 lookup tables created in
--           111_CreateLookupTables_Level2.sql.
--
-- VALUE SOURCE — <Entity>Code MUST equal the value persisted today:
--   tblDocumentCategory : enum names of FamilyFirst.Domain/Enums/DocumentCategory.cs
--                         SortOrder = the enum's int value (1–8) so Code ↔ int stays stable.
--   tblHealthRecordType : constants of FamilyFirst.Domain/Enums/HealthRecordEventType.cs
--   tblSafeZoneType     : constants of FamilyFirst.Domain/Enums/SafeZoneType.cs
--   tblFinanceCategory  : constants of FamilyFirst.Domain/Enums/FinanceCategory.cs
--                         Names = Mobile BudgetManagerScreen CATEGORY_LABELS (emoji removed).
--   Display names elsewhere = Code split into words (presentation only).
--
-- If a C# enum/constant set changes, add the row here in a NEW script —
-- never edit this one.
--
-- Depends : 111_CreateLookupTables_Level2.sql
-- Idempotent: NOT EXISTS guard on Code — safe to re-run.
-- ============================================================

-- ── tblDocumentCategory ───────────────────────────────────────────────────
INSERT INTO dbo.tblDocumentCategory (DocumentCategoryName, DocumentCategoryCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.DocumentCategoryName, source.DocumentCategoryCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Medical',        N'Medical',         1),
    (N'Identity',       N'Identity',        2),
    (N'School',         N'School',          3),
    (N'Financial',      N'Financial',       4),
    (N'Insurance',      N'Insurance',       5),
    (N'Legal',          N'Legal',           6),
    (N'Certificates',   N'Certificates',    7),
    (N'Other',          N'Other',           8)
) AS source (DocumentCategoryName, DocumentCategoryCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblDocumentCategory t
    WHERE t.DocumentCategoryCode = source.DocumentCategoryCode AND t.IsDeleted = 0
);
GO

-- ── tblHealthRecordType ───────────────────────────────────────────────────
INSERT INTO dbo.tblHealthRecordType (HealthRecordTypeName, HealthRecordTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.HealthRecordTypeName, source.HealthRecordTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Prescription',   N'Prescription',    1),
    (N'Vaccination',    N'Vaccination',     2),
    (N'Hospital Visit', N'HospitalVisit',   3),
    (N'Test Report',    N'TestReport',      4),
    (N'Doctor Note',    N'DoctorNote',      5),
    (N'Allergy Update', N'AllergyUpdate',   6)
) AS source (HealthRecordTypeName, HealthRecordTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblHealthRecordType t
    WHERE t.HealthRecordTypeCode = source.HealthRecordTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblSafeZoneType ───────────────────────────────────────────────────────
INSERT INTO dbo.tblSafeZoneType (SafeZoneTypeName, SafeZoneTypeCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.SafeZoneTypeName, source.SafeZoneTypeCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Home',               N'Home',            1),
    (N'School',             N'School',          2),
    (N'Tuition',            N'Tuition',         3),
    (N'Relatives House',    N'RelativesHouse',  4),
    (N'Workplace',          N'Workplace',       5),
    (N'Place of Worship',   N'PlaceOfWorship',  6),
    (N'Other',              N'Other',           7)
) AS source (SafeZoneTypeName, SafeZoneTypeCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblSafeZoneType t
    WHERE t.SafeZoneTypeCode = source.SafeZoneTypeCode AND t.IsDeleted = 0
);
GO

-- ── tblFinanceCategory ────────────────────────────────────────────────────
INSERT INTO dbo.tblFinanceCategory (FinanceCategoryName, FinanceCategoryCode, SortOrder, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT source.FinanceCategoryName, source.FinanceCategoryCode, source.SortOrder, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    (N'Groceries & Kirana',     N'GroceriesKirana',     1),
    (N'Food & Dining',          N'FoodDining',          2),
    (N'Utilities',              N'Utilities',           3),
    (N'Mobile Recharge',        N'MobileRecharge',      4),
    (N'Education & School',     N'EducationSchool',     5),
    (N'Medical & Health',       N'MedicalHealth',       6),
    (N'Travel & Transport',     N'TravelTransport',     7),
    (N'Shopping',               N'Shopping',            8),
    (N'Insurance & LIC',        N'InsuranceLIC',        9),
    (N'Loan EMI',               N'LoanEmi',            10),
    (N'Domestic Help',          N'DomesticHelp',       11),
    (N'Entertainment',          N'Entertainment',      12),
    (N'Donations',              N'DonationsReligion',  13),
    (N'Chit Fund / Investment', N'ChitFundInvestment', 14)
) AS source (FinanceCategoryName, FinanceCategoryCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblFinanceCategory t
    WHERE t.FinanceCategoryCode = source.FinanceCategoryCode AND t.IsDeleted = 0
);
GO
