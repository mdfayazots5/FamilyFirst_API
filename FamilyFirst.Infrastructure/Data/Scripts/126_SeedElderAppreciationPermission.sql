SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (Elder "Send appreciation" 403)
-- Date Created     : 23 Nov 2026
-- Description      : Grants Elder (6) · Feedback (6) · CreateUpdate (2). FeedbackService.SubmitFeedbackAsync checks
--                    this permission before its Elder rule (Appreciation type only, auto-created author profile),
--                    so ElderSendAppreciationScreen always returned 403. The service still rejects any non-
--                    Appreciation type from an Elder. Idempotent. Restart the API afterwards (permission cache).
-- Usage            : sqlcmd -I -b -i 126_SeedElderAppreciationPermission.sql
-- Input Parameters : NA
-- Output Parameters: NA
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      23 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------

INSERT INTO dbo.tblRolePermission
    (RoleId, ModuleId, PermissionId, IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT 6, 6, 2, 1, 1, 0, GETUTCDATE(), N'System'
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.tblRolePermission WITH (UPDLOCK, HOLDLOCK)
    WHERE RoleId = 6
      AND ModuleId = 6
      AND PermissionId = 2
      AND IsDeleted = 0
);
GO
