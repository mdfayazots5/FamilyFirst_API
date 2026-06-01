SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : Returns the user-facing error/success message for a given error code integer.
--                    Called in the finally block of every service method instead of returning
--                    hardcoded strings. Supports future multilingual messages via @LanguageId.
--                    Level 1: @LanguageId = 1 (English) only.
--                    If ErrorCode is not found, returns a generic technical error message.
-- Usage            : EXEC dbo.uspGetErrorCodeById @ErrorCode = 7, @LanguageId = 1
-- Input Parameters : @ErrorCode  — the integer error code (matches ErrorCode enum in FamilyFirstEnums.cs)
--                    @LanguageId — language for the message (default 1 = English)
-- Output           : ErrorCode, ReturnCode, ReturnMessage
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspGetErrorCodeById
(
    @ErrorCode      INT = 0,
    @LanguageId     INT = 1
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ec.ErrorCode,
        ec.ReturnCode,
        ec.ReturnMessage
    FROM dbo.tblErrorCode ec WITH (NOLOCK)
    WHERE ec.ErrorCode  = @ErrorCode
      AND ec.LanguageId = @LanguageId
      AND ec.IsDeleted  = 0
      AND ec.IsPublished = 1;

    -- If not found, return a generic technical error message
    IF @@ROWCOUNT = 0
    BEGIN
        SELECT
            @ErrorCode                                 AS ErrorCode,
            @ErrorCode                                 AS ReturnCode,
            N'A technical error occurred. Code: '
            + CAST(@ErrorCode AS NVARCHAR(16))         AS ReturnMessage;
    END
END
GO
