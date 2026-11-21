SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (R2 auth hardening)
-- Date Created     : 21 Nov 2026
-- Description      : Adds credential-login lockout tracking to tblUser (PIN and password login) and seeds the
--                    database-driven messages for a locked account and per-IP request throttling.
--                    Policy (enforced in AuthService): 5 consecutive failures -> locked for 15 minutes;
--                    a successful login resets the counter. Idempotent.
-- Usage            : sqlcmd -I -b -i 120_AlterUser_AddLoginLockout.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      21 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblUser') AND name = N'FailedLoginAttemptCount'
)
BEGIN
    ALTER TABLE dbo.tblUser
        ADD FailedLoginAttemptCount INT NOT NULL
            CONSTRAINT DF_tblUser_FailedLoginAttemptCount DEFAULT (0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblUser') AND name = N'LockoutUntil'
)
BEGIN
    ALTER TABLE dbo.tblUser
        ADD LockoutUntil DATETIME2 NULL;
END
GO

INSERT INTO dbo.tblErrorCode
    (ErrorCode, ErrorName, ReturnCode, ReturnMessage, LanguageId, DateCreated, CreatedBy)
SELECT source.ErrorCode, source.ErrorName, source.ErrorCode, source.ReturnMessage, 1, GETUTCDATE(), N'System'
FROM (VALUES
    (41, N'Account_Locked', N'Too many failed sign-in attempts. Try again in 15 minutes.'),
    (42, N'Too_Many_Requests', N'Too many requests. Please wait a moment and try again.')
) AS source (ErrorCode, ErrorName, ReturnMessage)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblErrorCode ec
    WHERE ec.ErrorCode = source.ErrorCode AND ec.LanguageId = 1 AND ec.IsDeleted = 0
);
GO
