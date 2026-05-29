IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_PhoneNumber_OtpLookup' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE INDEX IX_Users_PhoneNumber_OtpLookup
        ON dbo.Users (PhoneNumber)
        INCLUDE (UserId, IsPhoneVerified, IsActive)
        WHERE IsDeleted = 0;
END;
GO
