IF NOT EXISTS (SELECT 1 FROM dbo.tblFeatureFlag WHERE FlagKey = N'MaintenanceMode')
BEGIN
    INSERT INTO dbo.tblFeatureFlag (FlagKey, FlagValue, Description, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES (N'MaintenanceMode', N'false', N'Returns 503 for non-admin traffic when enabled.', 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblFeatureFlag WHERE FlagKey = N'MinimumAppVersion')
BEGIN
    INSERT INTO dbo.tblFeatureFlag (FlagKey, FlagValue, Description, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES (N'MinimumAppVersion', N'1.0.0', N'Minimum supported mobile app version.', 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblFeatureFlag WHERE FlagKey = N'GlobalNotifications')
BEGIN
    INSERT INTO dbo.tblFeatureFlag (FlagKey, FlagValue, Description, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES (N'GlobalNotifications', N'true', N'Global toggle for platform notification features.', 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblFeatureFlag WHERE FlagKey = N'GlobalReports')
BEGIN
    INSERT INTO dbo.tblFeatureFlag (FlagKey, FlagValue, Description, CompanyId, SiteId, CreatedBy, IPAddress)
    VALUES (N'GlobalReports', N'true', N'Global toggle for reporting features.', 1, 1, N'System', N'127.0.0.1');
END;
GO
