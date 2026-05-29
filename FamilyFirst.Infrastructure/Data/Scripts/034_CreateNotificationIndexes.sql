IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS idx
    WHERE idx.name = N'IX_Notifications_RecipientUserId_IsRead_IsSent'
        AND idx.object_id = OBJECT_ID(N'dbo.Notifications')
)
BEGIN
    CREATE INDEX IX_Notifications_RecipientUserId_IsRead_IsSent
        ON dbo.Notifications
        (
            RecipientUserId,
            IsRead,
            IsSent
        );
END;
GO
