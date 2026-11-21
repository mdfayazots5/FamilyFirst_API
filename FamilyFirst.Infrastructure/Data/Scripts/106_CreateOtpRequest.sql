SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.tblOtpRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblOtpRequest
    (
        OtpRequestId BIGINT IDENTITY(1,1) NOT NULL,
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tblOtpRequest_Id DEFAULT (NEWID()),
        CompanyId INT NOT NULL CONSTRAINT DF_tblOtpRequest_CompanyId DEFAULT (1),
        SiteId INT NOT NULL CONSTRAINT DF_tblOtpRequest_SiteId DEFAULT (1),
        DepartmentId INT NULL,
        PhoneNumberHash BINARY(32) NOT NULL,
        TokenHash BINARY(32) NOT NULL,
        OtpHash BINARY(32) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_tblOtpRequest_AttemptCount DEFAULT (0),
        IsConsumed BIT NOT NULL CONSTRAINT DF_tblOtpRequest_IsConsumed DEFAULT (0),
        Tag NVARCHAR(64) NULL,
        Comments NVARCHAR(256) NULL,
        DisplayOnWeb BIT NOT NULL CONSTRAINT DF_tblOtpRequest_DisplayOnWeb DEFAULT (1),
        IsPublished BIT NOT NULL CONSTRAINT DF_tblOtpRequest_IsPublished DEFAULT (1),
        DatePublished DATETIME2 NULL,
        PublishedBy NVARCHAR(128) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_tblOtpRequest_SortOrder DEFAULT (0),
        IPAddress NVARCHAR(64) NOT NULL CONSTRAINT DF_tblOtpRequest_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy NVARCHAR(128) NOT NULL CONSTRAINT DF_tblOtpRequest_CreatedBy DEFAULT (N'Admin'),
        DateCreated DATETIME2 NOT NULL CONSTRAINT DF_tblOtpRequest_DateCreated DEFAULT (GETUTCDATE()),
        UpdatedBy NVARCHAR(128) NULL,
        LastUpdated DATETIME2 NULL,
        DeletedBy NVARCHAR(128) NULL,
        DateDeleted DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_tblOtpRequest_IsDeleted DEFAULT (0),
        CONSTRAINT PK_tblOtpRequest_OtpRequestId PRIMARY KEY (OtpRequestId),
        CONSTRAINT CK_tblOtpRequest_AttemptCount CHECK (AttemptCount >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.tblOtpRequest')
      AND i.name = N'IDX_tblOtpRequest_PhoneNumberHash_DateCreated'
)
    CREATE INDEX IDX_tblOtpRequest_PhoneNumberHash_DateCreated
        ON dbo.tblOtpRequest (PhoneNumberHash, DateCreated) WHERE IsDeleted = 0;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.tblOtpRequest')
      AND i.name = N'UK_tblOtpRequest_TokenHash'
)
    CREATE UNIQUE INDEX UK_tblOtpRequest_TokenHash ON dbo.tblOtpRequest (TokenHash);
GO

-------------------------------------------------------------------------------------------------------------
-- Created By       : Codex
-- Date Created     : 20 Nov 2026
-- Description      : Apply the rolling request limit and persist an OTP challenge atomically.
-- Usage            : Execute through IOtpStore.TryCreateAsync.
-- Input Parameters : Phone digest, token digest, OTP MAC, window and lifetime limits.
-- Output Parameters: Id of the created request, or no row when the window is full.
-------------------------------------------------------------------------------------------------------------
-- Version   Author    Date         Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Codex     20 Nov 2026  Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspInsertOtpRequest
    @PhoneNumberHash BINARY(32),
    @TokenHash BINARY(32),
    @OtpHash BINARY(32),
    @WindowMinutes INT,
    @RequestLimit INT,
    @LifetimeMinutes INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRAN;
        DECLARE @LockResult INT;
        DECLARE @Resource NVARCHAR(255) = N'OtpRequest:' + CONVERT(VARCHAR(64), @PhoneNumberHash, 2);
        EXEC @LockResult = sys.sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction';
        IF @LockResult < 0
        BEGIN
            ROLLBACK TRAN;
            RETURN;
        END;

        DECLARE @UtcNow DATETIME2 = SYSUTCDATETIME();
        DECLARE @WindowStart DATETIME2 = DATEADD(MINUTE, -@WindowMinutes, @UtcNow);
        IF (
            SELECT COUNT(1)
            FROM dbo.tblOtpRequest r
            WHERE r.PhoneNumberHash = @PhoneNumberHash
              AND r.DateCreated >= @WindowStart
              AND r.IsDeleted = 0
        ) >= @RequestLimit
        BEGIN
            COMMIT TRAN;
            RETURN;
        END;

        INSERT INTO dbo.tblOtpRequest (Id, PhoneNumberHash, TokenHash, OtpHash, ExpiresAt, DateCreated, IsDeleted)
        VALUES (@NewId, @PhoneNumberHash, @TokenHash, @OtpHash, DATEADD(MINUTE, @LifetimeMinutes, @UtcNow), @UtcNow, 0);

        COMMIT TRAN;
        SELECT @NewId AS Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        THROW;
    END CATCH;
END;
GO

-------------------------------------------------------------------------------------------------------------
-- Created By       : Codex
-- Date Created     : 20 Nov 2026
-- Description      : Consume a matching OTP or increment the wrong-attempt counter in one update.
-- Usage            : Execute through IOtpStore.TryVerifyAsync.
-- Input Parameters : Phone digest, token digest, attempted OTP MAC, and attempt limit.
-- Output Parameters: Request GUID when the attempt consumes the OTP.
-------------------------------------------------------------------------------------------------------------
-- Version   Author    Date         Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Codex     20 Nov 2026  Creation
-------------------------------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.uspUpdateOtpRequestByTokenHash
    @PhoneNumberHash BINARY(32),
    @TokenHash BINARY(32),
    @OtpHash BINARY(32),
    @AttemptLimit INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Updated TABLE (Id UNIQUEIDENTIFIER, IsConsumed BIT);
    DECLARE @UtcNow DATETIME2 = SYSUTCDATETIME();

    UPDATE r
    SET AttemptCount = r.AttemptCount + CASE WHEN r.OtpHash = @OtpHash THEN 0 ELSE 1 END,
        IsConsumed = CASE WHEN r.OtpHash = @OtpHash THEN 1 ELSE 0 END,
        LastUpdated = @UtcNow,
        UpdatedBy = N'System'
    OUTPUT inserted.Id, inserted.IsConsumed INTO @Updated (Id, IsConsumed)
    FROM dbo.tblOtpRequest r
    WHERE r.TokenHash = @TokenHash
      AND r.PhoneNumberHash = @PhoneNumberHash
      AND r.ExpiresAt > @UtcNow
      AND r.IsConsumed = 0
      AND r.AttemptCount < @AttemptLimit
      AND r.IsDeleted = 0;

    SELECT u.Id FROM @Updated u WHERE u.IsConsumed = 1;
END;
GO
