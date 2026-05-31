IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IDX_tblNotification_RecipientUserId_IsRead_IsSent'
        AND idx.object_id = OBJECT_ID(N'dbo.tblNotification')
)
BEGIN
    CREATE INDEX IDX_tblNotification_RecipientUserId_IsRead_IsSent
        ON dbo.tblNotification
        (
            RecipientUserId,
            IsRead,
            IsSent
        );
END;
GO
