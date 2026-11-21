SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Codex (P10A)
-- Date Created     : 21 Nov 2026
-- Description      : Widens tblReportExport.DownloadUrl to NVARCHAR(4000). Originally sized for presigned URLs;
--                    since R5 only the canonical object URL is persisted, but the width is kept. Idempotent.
-- Usage            : sqlcmd -I -b -i 117_ExpandReportExportDownloadUrl.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Codex            21 Nov 2026    Creation
-- 1.1       FamilyFirst      21 Nov 2026    R8: added standard header block (no behaviour change)
-------------------------------------------------------------------------------------------------------------

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblReportExport', N'U')
      AND name = N'DownloadUrl'
      AND max_length = 2048
)
BEGIN
    ALTER TABLE dbo.tblReportExport ALTER COLUMN DownloadUrl NVARCHAR(4000) NULL;
END;
GO
