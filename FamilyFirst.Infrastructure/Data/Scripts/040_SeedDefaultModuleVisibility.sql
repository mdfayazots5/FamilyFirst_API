IF NOT EXISTS (SELECT 1 FROM dbo.tblModuleVisibilityConfig WHERE FamilyId IS NULL)
BEGIN
    INSERT INTO dbo.tblModuleVisibilityConfig
    (
        FamilyId, RoleId, ModuleName, IsVisible,
        CompanyId, SiteId, CreatedBy, IPAddress
    )
    VALUES
        (NULL, 2, N'Family',        1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Children',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Attendance',    1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Tasks',         1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Rewards',       1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Feedback',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Calendar',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Reports',       1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'Notifications', 1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 2, N'FamilyAdmin',   1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Family',        1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Children',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Attendance',    1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Tasks',         1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Rewards',       1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Feedback',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Calendar',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Reports',       1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 3, N'Notifications', 1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 4, N'Children',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 4, N'Attendance',    1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 4, N'Tasks',         1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 4, N'Rewards',       1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 4, N'Calendar',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 5, N'Attendance',    1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 5, N'Feedback',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 5, N'Calendar',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 5, N'Notifications', 1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 6, N'Family',        1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 6, N'Calendar',      1, 1, 1, N'System', N'127.0.0.1'),
        (NULL, 6, N'Notifications', 1, 1, 1, N'System', N'127.0.0.1');
END;
GO
