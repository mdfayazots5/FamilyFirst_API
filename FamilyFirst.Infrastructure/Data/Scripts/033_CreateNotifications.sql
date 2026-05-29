IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        NotificationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY DEFAULT NEWID(),
        FamilyId UNIQUEIDENTIFIER NULL,
        RecipientUserId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Body NVARCHAR(1000) NOT NULL,
        Priority INT NOT NULL CONSTRAINT DF_Notifications_Priority DEFAULT 2,
        Channel INT NOT NULL CONSTRAINT DF_Notifications_Channel DEFAULT 1,
        ReferenceType NVARCHAR(50) NULL,
        ReferenceId UNIQUEIDENTIFIER NULL,
        DeepLinkPath NVARCHAR(300) NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
        ReadAt DATETIME2 NULL,
        IsSent BIT NOT NULL CONSTRAINT DF_Notifications_IsSent DEFAULT 0,
        SentAt DATETIME2 NULL,
        FcmMessageId NVARCHAR(200) NULL,
        IsBatched BIT NOT NULL CONSTRAINT DF_Notifications_IsBatched DEFAULT 0,
        BatchGroup NVARCHAR(50) NULL,
        ScheduledFor DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Notifications_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES dbo.Families (FamilyId),
        CONSTRAINT FK_Notifications_Users_RecipientUserId FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO
