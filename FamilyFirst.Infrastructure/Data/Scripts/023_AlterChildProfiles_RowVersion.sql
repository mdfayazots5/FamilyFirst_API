IF COL_LENGTH(N'dbo.ChildProfiles', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.ChildProfiles
    ADD RowVersion ROWVERSION NOT NULL;
END;
GO
