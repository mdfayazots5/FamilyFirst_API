-- Covering index for OTP/PIN login lookup by phone number
-- Returns Id (GUID for API), IsPhoneVerified, IsActive without a key lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblUser_PhoneNumber_OtpLookup' AND object_id = OBJECT_ID(N'dbo.tblUser'))
BEGIN
    CREATE INDEX IDX_tblUser_PhoneNumber_OtpLookup
        ON dbo.tblUser (PhoneNumber)
        INCLUDE (Id, IsPhoneVerified, IsActive)
        WHERE IsDeleted = 0;
END;
GO
