USE [AstraSystemsRental];
GO

IF SCHEMA_ID(N'companies') IS NULL EXEC(N'CREATE SCHEMA [companies]');
GO

IF OBJECT_ID(N'companies.Companies', N'U') IS NULL
BEGIN
    CREATE TABLE [companies].[Companies]
    (
        [Id]             BIGINT        IDENTITY(1,1) NOT NULL,
        [Name]           NVARCHAR(160) NOT NULL,
        [DocumentNumber] VARCHAR(40)   NOT NULL,
        [Email]          NVARCHAR(256) NULL,
        [IsActive]       BIT           NOT NULL CONSTRAINT [DF_Companies_IsActive] DEFAULT (1),
        [CreatedAtUtc]   DATETIME2(3)  NOT NULL CONSTRAINT [DF_Companies_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

IF OBJECT_ID(N'companies.CompanyMembers', N'U') IS NULL
BEGIN
    CREATE TABLE [companies].[CompanyMembers]
    (
        [CompanyId]  BIGINT       NOT NULL,
        [UserId]     BIGINT       NOT NULL,
        [IsOwner]    BIT          NOT NULL CONSTRAINT [DF_CompanyMembers_IsOwner] DEFAULT (0),
        [JoinedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_CompanyMembers_JoinedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CompanyMembers] PRIMARY KEY CLUSTERED ([CompanyId], [UserId]),
        CONSTRAINT [FK_CompanyMembers_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [companies].[Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CompanyMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [users].[Users] ([Id])
    );
END
GO
