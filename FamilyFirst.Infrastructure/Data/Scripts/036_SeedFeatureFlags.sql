IF NOT EXISTS (SELECT 1 FROM dbo.FeatureFlags WHERE FlagKey = N'MaintenanceMode')
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagKey, FlagValue, Description)
    VALUES (N'MaintenanceMode', N'false', N'Returns 503 for non-admin traffic when enabled.');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.FeatureFlags WHERE FlagKey = N'MinimumAppVersion')
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagKey, FlagValue, Description)
    VALUES (N'MinimumAppVersion', N'1.0.0', N'Minimum supported mobile app version.');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.FeatureFlags WHERE FlagKey = N'GlobalNotifications')
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagKey, FlagValue, Description)
    VALUES (N'GlobalNotifications', N'true', N'Global toggle for platform notification features.');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.FeatureFlags WHERE FlagKey = N'GlobalReports')
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagKey, FlagValue, Description)
    VALUES (N'GlobalReports', N'true', N'Global toggle for reporting features.');
END;
GO
