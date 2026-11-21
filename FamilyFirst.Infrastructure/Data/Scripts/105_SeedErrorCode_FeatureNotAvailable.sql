SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Created By       : Codex
-- Date Created     : 20 Nov 2026
-- Description      : Seed the English error message for unavailable features.
-- Usage            : Run after 084_SeedErrorCodes.sql.
-- Input Parameters : NA
-- Output Parameters: NA
-- Version  Author  Date         Remarks
-- 1.0      Codex   20 Nov 2026  Creation

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.tblErrorCode ec
    WHERE ec.ErrorCode = 31
      AND ec.LanguageId = 1
      AND ec.IsDeleted = 0
)
BEGIN
    INSERT INTO dbo.tblErrorCode
        (ErrorCode, ErrorName, ReturnCode, ReturnMessage, LanguageId,
         IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
    VALUES
        (31, N'Feature_Not_Available', 31, N'This feature is not available yet.', 1,
         1, 1, 0, GETUTCDATE(), N'System');
END;
GO
