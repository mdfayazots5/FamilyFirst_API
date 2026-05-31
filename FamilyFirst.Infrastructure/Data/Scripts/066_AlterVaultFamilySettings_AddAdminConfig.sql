-- Extends tblVaultFamilySettings with Level 2 Advanced Admin Configuration columns.
-- No new tables — all L2 family-level admin config is stored here to avoid proliferating
-- single-row per-family config tables (AC-01 storage, AC-04 thresholds, DV-07 emergency, AC-06 finance privacy).

-- ── Storage Config (AC-01 / AC-02) ────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'StorageMode')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD StorageMode NVARCHAR(24) NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_StorageMode DEFAULT (N'AppManaged');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'StorageQuotaAlertThresholdPct')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD StorageQuotaAlertThresholdPct INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_StorageQuotaAlertThresholdPct DEFAULT (90);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'OfflineCacheSizeMb')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD OfflineCacheSizeMb INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_OfflineCacheSizeMb DEFAULT (500);
END;
GO

-- JSON array of { "Category": "...", "Provider": "App" | "GoogleDrive" }
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'HybridRoutingJson')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD HybridRoutingJson NVARCHAR(MAX) NULL;
END;
GO

-- ── Emergency Config (DV-07) ──────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'EmergencyLinkExpiryHours')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD EmergencyLinkExpiryHours INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_EmergencyLinkExpiryHours DEFAULT (72);
END;
GO

-- JSON array of { "Name": "...", "PhoneNumber": "...", "Relationship": "..." } — max 3 contacts
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'EmergencyContactsJson')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD EmergencyContactsJson NVARCHAR(1024) NULL;
END;
GO

-- ── Alert Thresholds (AC-04 extended) ─────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'FinanceLargeTransactionThreshold')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD FinanceLargeTransactionThreshold MONEY NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_FinanceLargeTransactionThreshold DEFAULT (5000.00);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DocExpiryLeadDaysDefault')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DocExpiryLeadDaysDefault INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DocExpiryLeadDaysDefault DEFAULT (30);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DocExpiryLeadDaysIdentity')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DocExpiryLeadDaysIdentity INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DocExpiryLeadDaysIdentity DEFAULT (60);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DocExpiryLeadDaysMedical')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DocExpiryLeadDaysMedical INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DocExpiryLeadDaysMedical DEFAULT (30);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DocExpiryLeadDaysInsurance')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DocExpiryLeadDaysInsurance INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DocExpiryLeadDaysInsurance DEFAULT (45);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'LateArrivalToleranceMinutes')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD LateArrivalToleranceMinutes INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_LateArrivalToleranceMinutes DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'LocationStaleThresholdMinutes')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD LocationStaleThresholdMinutes INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_LocationStaleThresholdMinutes DEFAULT (60);
END;
GO

-- ── Finance Privacy Config (AC-06) ────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DefaultAdultEarningMemberTier')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DefaultAdultEarningMemberTier INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DefaultAdultEarningMemberTier DEFAULT (2);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'DefaultIndependentMemberTier')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD DefaultIndependentMemberTier INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_DefaultIndependentMemberTier DEFAULT (3);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'ConsentReminderIntervalDays')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD ConsentReminderIntervalDays INT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_ConsentReminderIntervalDays DEFAULT (30);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblVaultFamilySettings') AND name = N'AutoExcludeSalaryCredits')
BEGIN
    ALTER TABLE dbo.tblVaultFamilySettings
    ADD AutoExcludeSalaryCredits BIT NOT NULL
        CONSTRAINT DF_tblVaultFamilySettings_AutoExcludeSalaryCredits DEFAULT (1);
END;
GO
