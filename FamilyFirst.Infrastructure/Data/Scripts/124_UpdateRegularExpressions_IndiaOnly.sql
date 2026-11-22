SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (India-only / no join code)
-- Date Created     : 22 Nov 2026
-- Description      : Founder decision 2026-11-22 — FamilyFirst is India-only and join-code onboarding is removed.
--                    1. PhoneNumber rules for SendOtp / VerifyOtp / AddMember -> Indian mobile only:
--                       10 digits starting with 6-9, optional +91 prefix  ( ^(\+91)?[6-9]\d{9}$ ).
--                    2. SendOtp CountryCode rule -> +91 only  ( ^\+91$ ).
--                    3. Soft-deletes the JoinFamily API method and its regex rules (POST /api/families/join
--                       no longer exists).
--                    Idempotent: re-running changes nothing once the patterns match.
--                    CacheWarmupService loads these rules at startup — restart the API after running.
-- Usage            : sqlcmd -I -b -i 124_UpdateRegularExpressions_IndiaOnly.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      22 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------

UPDATE rx
SET    rx.RegexPattern = N'^(\+91)?[6-9]\d{9}$',
       rx.Description  = N'Indian mobile number: 10 digits starting with 6-9, optional +91 prefix',
       rx.LastUpdated  = GETDATE(),
       rx.UpdatedBy    = N'System'
FROM   dbo.tblRegularExpression rx
INNER JOIN dbo.tblAPIMethod am
       ON am.APIMethodId = rx.APIMethodId
WHERE  rx.IsDeleted = 0
  AND  am.IsDeleted = 0
  AND  am.MethodName IN (N'SendOtp', N'VerifyOtp', N'AddMember')
  AND  rx.FieldName = N'PhoneNumber'
  AND  rx.RegexPattern <> N'^(\+91)?[6-9]\d{9}$';
GO

UPDATE rx
SET    rx.RegexPattern = N'^\+91$',
       rx.Description  = N'India only: country code must be +91',
       rx.LastUpdated  = GETDATE(),
       rx.UpdatedBy    = N'System'
FROM   dbo.tblRegularExpression rx
INNER JOIN dbo.tblAPIMethod am
       ON am.APIMethodId = rx.APIMethodId
WHERE  rx.IsDeleted = 0
  AND  am.IsDeleted = 0
  AND  am.MethodName = N'SendOtp'
  AND  rx.FieldName = N'CountryCode'
  AND  rx.RegexPattern <> N'^\+91$';
GO

UPDATE rx
SET    rx.IsDeleted   = 1,
       rx.DateDeleted = GETDATE(),
       rx.DeletedBy   = N'System'
FROM   dbo.tblRegularExpression rx
INNER JOIN dbo.tblAPIMethod am
       ON am.APIMethodId = rx.APIMethodId
WHERE  rx.IsDeleted = 0
  AND  am.MethodName = N'JoinFamily';
GO

UPDATE am
SET    am.IsDeleted   = 1,
       am.DateDeleted = GETDATE(),
       am.DeletedBy   = N'System'
FROM   dbo.tblAPIMethod am
WHERE  am.IsDeleted = 0
  AND  am.MethodName = N'JoinFamily';
GO
