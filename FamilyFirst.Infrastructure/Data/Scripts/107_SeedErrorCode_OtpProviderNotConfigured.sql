SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1 FROM dbo.tblErrorCode e
    WHERE e.ErrorCode = 32 AND e.LanguageId = 1 AND e.IsDeleted = 0
)
BEGIN
    INSERT INTO dbo.tblErrorCode (ErrorCode, ErrorName, ReturnCode, ReturnMessage, LanguageId, DateCreated, CreatedBy)
    VALUES (32, N'OTP_Provider_Not_Configured', 32, N'OTP provider is not configured.', 1, GETUTCDATE(), N'System');
END;
GO
