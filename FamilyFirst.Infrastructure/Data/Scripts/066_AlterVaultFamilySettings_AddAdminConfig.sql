-- Extends VaultFamilySettings with Level 2 Advanced Admin Configuration columns.
-- No new tables — all L2 family-level admin config is stored here to avoid proliferating
-- single-row per-family config tables (AC-01 storage, AC-04 thresholds, DV-07 emergency, AC-06 finance privacy).

-- ── Storage Config (AC-01 / AC-02) ────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'StorageMode')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD StorageMode NVARCHAR(20) NOT NULL CONSTRAINT DF_VaultFamilySettings_StorageMode DEFAULT N'AppManaged';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'StorageQuotaAlertThresholdPct')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD StorageQuotaAlertThresholdPct INT NOT NULL CONSTRAINT DF_VaultFamilySettings_QuotaAlert DEFAULT 90;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'OfflineCacheSizeMb')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD OfflineCacheSizeMb INT NOT NULL CONSTRAINT DF_VaultFamilySettings_CacheSize DEFAULT 500;
END;
GO

-- JSON array of { "Category": "...", "Provider": "App" | "GoogleDrive" }
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'HybridRoutingJson')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD HybridRoutingJson NVARCHAR(MAX) NULL;
END;
GO

-- ── Emergency Config (DV-07) ──────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'EmergencyLinkExpiryHours')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD EmergencyLinkExpiryHours INT NOT NULL CONSTRAINT DF_VaultFamilySettings_EmergencyExpiry DEFAULT 72;
END;
GO

-- JSON array of { "Name": "...", "PhoneNumber": "...", "Relationship": "..." } — max 3 contacts
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'EmergencyContactsJson')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD EmergencyContactsJson NVARCHAR(1000) NULL;
END;
GO

-- ── Alert Thresholds (AC-04 extended) ─────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'FinanceLargeTransactionThreshold')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD FinanceLargeTransactionThreshold DECIMAL(18,2) NOT NULL CONSTRAINT DF_VaultFamilySettings_FinanceThreshold DEFAULT 5000.00;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DocExpiryLeadDaysDefault')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DocExpiryLeadDaysDefault INT NOT NULL CONSTRAINT DF_VaultFamilySettings_DocLeadDefault DEFAULT 30;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DocExpiryLeadDaysIdentity')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DocExpiryLeadDaysIdentity INT NOT NULL CONSTRAINT DF_VaultFamilySettings_DocLeadIdentity DEFAULT 60;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DocExpiryLeadDaysMedical')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DocExpiryLeadDaysMedical INT NOT NULL CONSTRAINT DF_VaultFamilySettings_DocLeadMedical DEFAULT 30;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DocExpiryLeadDaysInsurance')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DocExpiryLeadDaysInsurance INT NOT NULL CONSTRAINT DF_VaultFamilySettings_DocLeadInsurance DEFAULT 45;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'LateArrivalToleranceMinutes')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD LateArrivalToleranceMinutes INT NOT NULL CONSTRAINT DF_VaultFamilySettings_LateArrivalTolerance DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'LocationStaleThresholdMinutes')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD LocationStaleThresholdMinutes INT NOT NULL CONSTRAINT DF_VaultFamilySettings_LocationStale DEFAULT 60;
END;
GO

-- ── Finance Privacy Config (AC-06) ────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DefaultAdultEarningMemberTier')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DefaultAdultEarningMemberTier INT NOT NULL CONSTRAINT DF_VaultFamilySettings_AdultTier DEFAULT 2;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'DefaultIndependentMemberTier')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD DefaultIndependentMemberTier INT NOT NULL CONSTRAINT DF_VaultFamilySettings_IndepTier DEFAULT 3;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'ConsentReminderIntervalDays')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD ConsentReminderIntervalDays INT NOT NULL CONSTRAINT DF_VaultFamilySettings_ConsentReminder DEFAULT 30;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VaultFamilySettings') AND name = N'AutoExcludeSalaryCredits')
BEGIN
    ALTER TABLE dbo.VaultFamilySettings
    ADD AutoExcludeSalaryCredits BIT NOT NULL CONSTRAINT DF_VaultFamilySettings_AutoExcludeSalary DEFAULT 1;
END;
GO
