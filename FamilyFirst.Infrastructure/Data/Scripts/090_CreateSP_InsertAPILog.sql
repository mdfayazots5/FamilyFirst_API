SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-------------------------------------------------------------------------------------------------------------
-- Created By       : Claude Project AI Engineer
-- Date Created     : 01 Jun 2026
-- Description      : Inserts an API request/response log record into tblAPILog.
--                    Called via Task.Run (fire-and-forget) from every service method's finally block.
--                    Does NOT block the HTTP response — the async task runs after the response is returned.
--                    Generates GUID internally per SQL Format standard. Returns new Id GUID.
-- Usage            : EXEC dbo.uspInsertAPILog
--                         @APIMethodId     = 1,
--                         @MethodName      = N'SubmitAttendance',
--                         @RequestJSON     = N'{"sessionId":"..."}',
--                         @ResponseJSON    = N'{"success":true}',
--                         @Token           = N'eyJhbGc...',
--                         @CreatedByUserId = 42,
--                         @IPAddress       = N'192.168.1.1',
--                         @CreatedBy       = N'teacher@school.com'
-- Input Parameters : See below
-- Output           : Id (UNIQUEIDENTIFIER) — the new log record's GUID
-------------------------------------------------------------------------------------------------------------
-- Version   Author                     Date           Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Claude Project AI Engineer 01 Jun 2026    Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspInsertAPILog
(
    @APIMethodId        BIGINT          = 0,
    @MethodName         NVARCHAR(256)   = NULL,
    @RequestJSON        NVARCHAR(MAX)   = NULL,
    @ResponseJSON       NVARCHAR(MAX)   = NULL,
    @Token              NVARCHAR(2048)  = NULL,
    @CreatedByUserId    BIGINT          = 0,
    @IPAddress          NVARCHAR(64)    = NULL,
    @CreatedBy          NVARCHAR(128)   = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        INSERT INTO dbo.tblAPILog
        (
            Id,
            APIMethodId,
            MethodName,
            RequestJSON,
            ResponseJSON,
            Token,
            CreatedByUserId,
            IPAddress,
            CreatedBy,
            DateCreated,
            CompanyId,
            SiteId
        )
        VALUES
        (
            @NewId,
            NULLIF(@APIMethodId, 0),
            @MethodName,
            @RequestJSON,
            @ResponseJSON,
            @Token,
            NULLIF(@CreatedByUserId, 0),
            ISNULL(@IPAddress, N'127.0.0.1'),
            ISNULL(@CreatedBy, N'System'),
            GETDATE(),
            1,
            1
        );

        SELECT @NewId AS Id;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
