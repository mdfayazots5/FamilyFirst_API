-- CFO questions + member replies for the transaction questioning flow.
-- Questions sent via WhatsApp/SMS (server-side) — language always curious, never accusatory.
IF OBJECT_ID(N'dbo.tblTransactionQuestion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTransactionQuestion
    (
        TransactionQuestionId   BIGINT IDENTITY(1,1) NOT NULL,
        Id                      UNIQUEIDENTIFIER NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_Id DEFAULT (NEWID()),
        CompanyId               INT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_CompanyId DEFAULT (1),
        SiteId                  INT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_SiteId DEFAULT (1),
        DepartmentId            INT NULL,

        -- Business columns
        FamilyId                BIGINT NOT NULL,
        TransactionId           BIGINT NOT NULL,
        -- QuestionType: FamilyExpense / PersonalUnderstood / NeedToKnowMore / PossibleError
        QuestionType            NVARCHAR(32) NOT NULL,
        -- CFO's optional context message
        ContextNote             NVARCHAR(512) NULL,
        -- When WhatsApp/SMS was dispatched
        MessageSentAt           DATETIME2 NOT NULL,
        -- Member's WhatsApp reply
        MemberReply             NVARCHAR(1024) NULL,
        ReplyReceivedAt         DATETIME2 NULL,
        -- ResolutionStatus: Resolved / FamilyExpense / Personal / UnderReview
        ResolutionStatus        NVARCHAR(24) NULL,
        ResolvedAt              DATETIME2 NULL,
        -- FK → tblUser.UserId (CFO)
        ResolvedByUserId        BIGINT NULL,

        -- Audit columns
        Tag                     NVARCHAR(64) NULL,
        Comments                NVARCHAR(256) NULL,
        DisplayOnWeb            BIT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_DisplayOnWeb DEFAULT (1),
        IsPublished             BIT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_IsPublished DEFAULT (1),
        DatePublished           DATETIME2 NULL,
        PublishedBy             NVARCHAR(128) NULL,
        SortOrder               INT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_SortOrder DEFAULT (0),
        IPAddress               NVARCHAR(64) NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_IPAddress DEFAULT (N'127.0.0.1'),
        CreatedBy               NVARCHAR(128) NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_CreatedBy DEFAULT (N'Admin'),
        DateCreated             DATETIME2 NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_DateCreated DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(128) NULL,
        LastUpdated             DATETIME2 NULL,
        DeletedBy               NVARCHAR(128) NULL,
        DateDeleted             DATETIME2 NULL,
        IsDeleted               BIT NOT NULL
                                    CONSTRAINT DF_tblTransactionQuestion_IsDeleted DEFAULT (0),

        CONSTRAINT PK_tblTransactionQuestion_TransactionQuestionId PRIMARY KEY (TransactionQuestionId),
        CONSTRAINT FK_tblTransactionQuestion_FamilyId_tblFamily_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES dbo.tblFamily (FamilyId),
        CONSTRAINT FK_tblTransactionQuestion_TransactionId_tblTransaction_TransactionId
            FOREIGN KEY (TransactionId) REFERENCES dbo.tblTransaction (TransactionId),
        CONSTRAINT FK_tblTransactionQuestion_ResolvedByUserId_tblUser_UserId
            FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.tblUser (UserId),
        CONSTRAINT CK_tblTransactionQuestion_QuestionType
            CHECK (QuestionType IN (N'FamilyExpense', N'PersonalUnderstood', N'NeedToKnowMore', N'PossibleError')),
        CONSTRAINT CK_tblTransactionQuestion_ResolutionStatus
            CHECK (ResolutionStatus IS NULL OR ResolutionStatus IN (N'Resolved', N'FamilyExpense', N'Personal', N'UnderReview'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UK_tblTransactionQuestion_Id' AND object_id = OBJECT_ID(N'dbo.tblTransactionQuestion'))
BEGIN
    CREATE UNIQUE INDEX UK_tblTransactionQuestion_Id ON dbo.tblTransactionQuestion (Id) WHERE IsDeleted = 0;
END;
GO

-- Questions awaiting reply — CFO unresolved queue
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_tblTransactionQuestion_TransactionId' AND object_id = OBJECT_ID(N'dbo.tblTransactionQuestion'))
BEGIN
    CREATE INDEX IDX_tblTransactionQuestion_TransactionId
        ON dbo.tblTransactionQuestion (TransactionId)
        WHERE IsDeleted = 0;
END;
GO
