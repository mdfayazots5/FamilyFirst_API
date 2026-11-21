SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Codex (P4)
-- Date Created     : 21 Nov 2026
-- Description      : Seeds tblErrorCode messages 33–40 used by the Level 2 services (consent, share link,
--                    emergency limit, health profile, concurrency, week start, date range, privacy tier). Idempotent.
-- Usage            : sqlcmd -I -b -i 116_SeedErrorCodes_Level2.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Codex            21 Nov 2026    Creation
-- 1.1       FamilyFirst      21 Nov 2026    R8: added standard header block (no behaviour change)
-------------------------------------------------------------------------------------------------------------

-- Seed database-driven messages used by the Level 2 services.
INSERT INTO dbo.tblErrorCode
    (ErrorCode, ErrorName, ReturnCode, ReturnMessage, LanguageId, DateCreated, CreatedBy)
SELECT source.ErrorCode, source.ErrorName, source.ErrorCode, source.ReturnMessage, 1, GETUTCDATE(), N'System'
FROM (VALUES
    (33, N'Consent_Required', N'The member must give consent before this operation.'),
    (34, N'Share_Link_Expired', N'The share link was not found or has expired.'),
    (35, N'Emergency_Document_Limit', N'The family is limited to five emergency documents.'),
    (36, N'Health_Profile_Incomplete', N'Add blood group and allergies before sharing the emergency card.'),
    (37, N'Concurrent_Update', N'This record changed during the operation. Please retry.'),
    (38, N'Invalid_Week_Start', N'The week start date must be a Monday.'),
    (39, N'Invalid_Date_Range', N'The start date must be on or before the end date.'),
    (40, N'Invalid_Privacy_Tier', N'Privacy tier must be 1, 2, or 3.')
) AS source (ErrorCode, ErrorName, ReturnMessage)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblErrorCode ec
    WHERE ec.ErrorCode = source.ErrorCode AND ec.LanguageId = 1 AND ec.IsDeleted = 0
);
GO
