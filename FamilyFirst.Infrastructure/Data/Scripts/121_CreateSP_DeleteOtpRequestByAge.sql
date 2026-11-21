SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Supports the age-based purge below (range scan on DateCreated, independent of IsDeleted).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.tblOtpRequest')
      AND i.name = N'IDX_tblOtpRequest_DateCreated'
)
BEGIN
    CREATE INDEX IDX_tblOtpRequest_DateCreated
        ON dbo.tblOtpRequest (DateCreated);
END
GO

-------------------------------------------------------------------------------------------------------------
-- Created By       : FamilyFirst Delivery (R2 auth hardening)
-- Date Created     : 21 Nov 2026
-- Description      : PHYSICALLY deletes one batch of OTP challenge rows created before @CutoffUtc and returns
--                    the number of rows removed. Physical delete approved by the founder (21 Nov 2026):
--                    rows hold only digests of single-use codes and have no audit value after 24 hours.
--                    The caller (OtpCleanupWorker) loops until a batch returns fewer than @BatchSize rows.
--                    @CutoffUtc must be older than the 60-minute OTP request window (worker uses now - 24h).
-- Usage            : EXEC dbo.uspDeleteOtpRequestByAge @CutoffUtc = '2026-11-20T00:00:00', @BatchSize = 5000
-- Input Parameters : @CutoffUtc - delete rows with DateCreated earlier than this UTC time
--                    @BatchSize - maximum rows to delete in this call
-- Output Parameters: NA (result set: DeletedCount)
-------------------------------------------------------------------------------------------------------------
-- Version   Author           Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       FamilyFirst      21 Nov 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspDeleteOtpRequestByAge
(
    @CutoffUtc  DATETIME2 = NULL,
    @BatchSize  INT = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @CutoffUtc IS NULL OR @BatchSize <= 0
    BEGIN
        SELECT 0 AS DeletedCount;
        RETURN;
    END

    BEGIN TRY
        DELETE TOP (@BatchSize) r
        FROM dbo.tblOtpRequest r
        WHERE r.DateCreated < @CutoffUtc;

        SELECT @@ROWCOUNT AS DeletedCount;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        THROW;
    END CATCH;
END
GO
