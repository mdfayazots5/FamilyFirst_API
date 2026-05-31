IF NOT EXISTS (SELECT 1 FROM dbo.tblPlan WHERE PlanCode = N'free_trial')
BEGIN
    INSERT INTO dbo.tblPlan
    (
        PlanName,
        PlanCode,
        PriceMonthly,
        MaxChildren,
        MaxTeachers,
        HasElderMode,
        HasWeeklyDigest,
        HasAdvancedReports,
        StorageQuotaMb,
        TrialDays,
        IsActive,
        CompanyId,
        SiteId,
        CreatedBy,
        IPAddress
    )
    VALUES (N'Free Trial', N'free_trial', 0.00, 1, 0, 0, 0, 0, 100, 14, 1, 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblPlan WHERE PlanCode = N'basic')
BEGIN
    INSERT INTO dbo.tblPlan
    (
        PlanName,
        PlanCode,
        PriceMonthly,
        MaxChildren,
        MaxTeachers,
        HasElderMode,
        HasWeeklyDigest,
        HasAdvancedReports,
        StorageQuotaMb,
        TrialDays,
        IsActive,
        CompanyId,
        SiteId,
        CreatedBy,
        IPAddress
    )
    VALUES (N'Basic', N'basic', 99.00, 2, 1, 0, 1, 0, 500, 0, 1, 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblPlan WHERE PlanCode = N'family')
BEGIN
    INSERT INTO dbo.tblPlan
    (
        PlanName,
        PlanCode,
        PriceMonthly,
        MaxChildren,
        MaxTeachers,
        HasElderMode,
        HasWeeklyDigest,
        HasAdvancedReports,
        StorageQuotaMb,
        TrialDays,
        IsActive,
        CompanyId,
        SiteId,
        CreatedBy,
        IPAddress
    )
    VALUES (N'Family', N'family', 199.00, 4, 2, 1, 1, 1, 2048, 0, 1, 1, 1, N'System', N'127.0.0.1');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblPlan WHERE PlanCode = N'premium')
BEGIN
    INSERT INTO dbo.tblPlan
    (
        PlanName,
        PlanCode,
        PriceMonthly,
        MaxChildren,
        MaxTeachers,
        HasElderMode,
        HasWeeklyDigest,
        HasAdvancedReports,
        StorageQuotaMb,
        TrialDays,
        IsActive,
        CompanyId,
        SiteId,
        CreatedBy,
        IPAddress
    )
    VALUES (N'Premium', N'premium', 299.00, 99, 10, 1, 1, 1, 10240, 0, 1, 1, 1, N'System', N'127.0.0.1');
END;
GO
