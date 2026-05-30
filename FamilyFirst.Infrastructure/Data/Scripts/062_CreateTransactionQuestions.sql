-- CFO questions + member replies for the transaction questioning flow.
-- Questions sent via WhatsApp/SMS (server-side) — language always curious, never accusatory.
IF OBJECT_ID(N'dbo.TransactionQuestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionQuestions
    (
        TransactionQuestionId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TransactionQuestions PRIMARY KEY DEFAULT NEWID(),
        FamilyId                UNIQUEIDENTIFIER NOT NULL,
        TransactionId           UNIQUEIDENTIFIER NOT NULL,
        -- QuestionType: FamilyExpense / PersonalUnderstood / NeedToKnowMore / PossibleError
        QuestionType            NVARCHAR(30)     NOT NULL,
        ContextNote             NVARCHAR(500)    NULL,       -- CFO's optional message
        MessageSentAt           DATETIME2        NOT NULL,   -- When WhatsApp/SMS was dispatched
        MemberReply             NVARCHAR(1000)   NULL,       -- Member's WhatsApp reply
        ReplyReceivedAt         DATETIME2        NULL,
        -- ResolutionStatus: Resolved / FamilyExpense / Personal / UnderReview
        ResolutionStatus        NVARCHAR(20)     NULL,
        ResolvedAt              DATETIME2        NULL,
        ResolvedByUserId        UNIQUEIDENTIFIER NULL,       -- FK → Users.UserId (CFO)
        CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_TransactionQuestions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2        NOT NULL CONSTRAINT DF_TransactionQuestions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted               BIT              NOT NULL CONSTRAINT DF_TransactionQuestions_IsDeleted  DEFAULT 0,
        DeletedAt               DATETIME2        NULL,

        CONSTRAINT FK_TransactionQuestions_Families_FamilyId
            FOREIGN KEY (FamilyId)       REFERENCES dbo.Families     (FamilyId),
        CONSTRAINT FK_TransactionQuestions_Transactions_TransactionId
            FOREIGN KEY (TransactionId)  REFERENCES dbo.Transactions (TransactionId),
        CONSTRAINT FK_TransactionQuestions_Users_ResolvedByUserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users      (UserId),
        CONSTRAINT CK_TransactionQuestions_QuestionType
            CHECK (QuestionType IN (N'FamilyExpense', N'PersonalUnderstood', N'NeedToKnowMore', N'PossibleError')),
        CONSTRAINT CK_TransactionQuestions_ResolutionStatus
            CHECK (ResolutionStatus IS NULL OR ResolutionStatus IN (N'Resolved', N'FamilyExpense', N'Personal', N'UnderReview'))
    );
END;
GO

-- Questions awaiting reply — CFO unresolved queue
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TransactionQuestions_TransactionId'
      AND object_id = OBJECT_ID(N'dbo.TransactionQuestions')
)
BEGIN
    CREATE INDEX IX_TransactionQuestions_TransactionId
        ON dbo.TransactionQuestions (TransactionId)
        WHERE IsDeleted = 0;
END;
GO
