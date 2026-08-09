USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'companies.CompanyInvitations', N'U') IS NULL
BEGIN
    CREATE TABLE [companies].[CompanyInvitations]
    (
        [Id]              BIGINT        IDENTITY(1,1) NOT NULL,
        [CompanyId]       BIGINT        NOT NULL,
        [Email]           NVARCHAR(256) NOT NULL,
        [TokenHash]       VARCHAR(128)  NOT NULL,
        [InvitedByUserId] BIGINT        NOT NULL,
        [Status]          VARCHAR(20)   NOT NULL CONSTRAINT [DF_CompanyInvitations_Status] DEFAULT ('Pending'),
        [ExpiresAtUtc]    DATETIME2(3)  NOT NULL,
        [SentCount]       INT           NOT NULL CONSTRAINT [DF_CompanyInvitations_SentCount] DEFAULT (1),
        [LastSentAtUtc]   DATETIME2(3)  NOT NULL CONSTRAINT [DF_CompanyInvitations_LastSentAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedAtUtc]    DATETIME2(3)  NOT NULL CONSTRAINT [DF_CompanyInvitations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [RespondedAtUtc]  DATETIME2(3)  NULL,
        CONSTRAINT [PK_CompanyInvitations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_CompanyInvitations_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [companies].[Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CompanyInvitations_InvitedBy] FOREIGN KEY ([InvitedByUserId]) REFERENCES [users].[Users] ([Id]),
        CONSTRAINT [CK_CompanyInvitations_Status] CHECK ([Status] IN ('Pending', 'Accepted', 'Revoked', 'Expired'))
    );

    CREATE UNIQUE INDEX [UX_CompanyInvitations_TokenHash] ON [companies].[CompanyInvitations] ([TokenHash]);
    CREATE INDEX [IX_CompanyInvitations_CompanyId_Status] ON [companies].[CompanyInvitations] ([CompanyId], [Status]);
    CREATE INDEX [IX_CompanyInvitations_Email_Status] ON [companies].[CompanyInvitations] ([Email], [Status]);
END
GO
