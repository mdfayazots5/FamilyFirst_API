IF COL_LENGTH(N'dbo.tblChildProfile', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.tblChildProfile
    ADD RowVersion ROWVERSION NOT NULL;
END;
GO
