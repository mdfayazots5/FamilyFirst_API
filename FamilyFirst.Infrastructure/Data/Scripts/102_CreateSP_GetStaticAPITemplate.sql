SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : Looks up tblStaticAPITemplate to resolve the stored procedure name for a given
--                    ModuleCode + MethodName combination. Called by the generic GetDataBySearch and
--                    GetDataByCode C# API controllers before executing the SP dynamically.
--
-- FLOW:
--   1. UI sends: { ModuleCode: "ATTEND", MethodName: "GetAttendanceSessionBySearch", ... }
--   2. Generic controller calls this SP to get StoredProcedureName
--   3. Controller executes the returned SP with the standard parameter set
--   4. Returns result to UI
--
-- If ModuleCode is NULL → template is global (e.g. MasterData, cross-module).
-- Empty result → BAL returns error 27 (Invalid_Module) or 10 (Missing_Parameters).
--
-- NO hardcoded error messages or return codes — Section 6B of New SQL Format.txt.
--
-- Usage : EXEC dbo.uspGetStaticAPITemplateByModuleCode
--              @ModuleCode = 'ATTEND',
--              @MethodName = 'GetAttendanceSessionBySearch'
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspGetStaticAPITemplateByModuleCode
(
    @ModuleCode     NVARCHAR(64)    = NULL,
    @MethodName     NVARCHAR(256)   = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        sat.StoredProcedureName,
        sat.StaticAPIMode,
        sat.ArrayListitem,
        sat.Id          AS TemplateGuid
    FROM dbo.tblStaticAPITemplate sat WITH (NOLOCK)
    LEFT JOIN dbo.tblModule m WITH (NOLOCK)
        ON m.ModuleId   = sat.ModuleId
       AND m.IsDeleted  = 0
    WHERE sat.IsDeleted         = 0
      AND sat.IsPublished        = 1
      AND sat.StaticAPIMethodName = @MethodName
      AND (
            -- Module-scoped entry
            (m.ModuleCode = @ModuleCode AND @ModuleCode IS NOT NULL)
            OR
            -- Global entry (no module scope — e.g. MasterData)
            (sat.ModuleId IS NULL AND @ModuleCode IS NULL)
          );
END
GO
