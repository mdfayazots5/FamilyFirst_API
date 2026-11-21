SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst
-- Date Created     : 21 Nov 2026
-- Description      : Returns configuration rows for the startup cache.
-- Usage            : EXEC dbo.uspGetStartupCacheData
-- Input Parameters : NA
-- Output Parameters: NA. Returns errors, permission grants, API methods, regex rules and master codes.
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-- 1.0       FamilyFirst      21 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspGetStartupCacheData
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ErrorCode, LanguageId, ReturnCode, ReturnMessage
    FROM dbo.tblErrorCode
    WHERE IsDeleted = 0 AND IsPublished = 1;

    SELECT RoleId, ModuleId, PermissionId
    FROM dbo.tblRolePermission
    WHERE IsDeleted = 0 AND IsPublished = 1;

    SELECT APIMethodId, MethodName
    FROM dbo.tblAPIMethod
    WHERE IsDeleted = 0 AND IsPublished = 1;

    SELECT expression.APIMethodId, expression.FieldName, expression.RegexPattern
    FROM dbo.tblRegularExpression AS expression
    INNER JOIN dbo.tblAPIMethod AS method ON method.APIMethodId = expression.APIMethodId
    WHERE expression.IsDeleted = 0 AND expression.IsPublished = 1
      AND method.IsDeleted = 0 AND method.IsPublished = 1
    ORDER BY expression.RegularExpressionId;

    SELECT MasterDataCode
    FROM dbo.tblMasterData
    WHERE IsDeleted = 0 AND IsPublished = 1
    ORDER BY SortOrder, MasterDataCode;
END
GO
