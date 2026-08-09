USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'subscriptions.Plans', N'U') IS NULL
BEGIN
    CREATE TABLE [subscriptions].[Plans]
    (
        [Id]          INT           IDENTITY(1,1) NOT NULL,
        [Code]        VARCHAR(40)   NOT NULL,
        [Name]        NVARCHAR(120) NOT NULL,
        [DurationDays] INT          NOT NULL,
        [IsActive]    BIT           NOT NULL CONSTRAINT [DF_Plans_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Plans_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Plans] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

IF OBJECT_ID(N'subscriptions.PlanNodes', N'U') IS NULL
BEGIN
    CREATE TABLE [subscriptions].[PlanNodes]
    (
        [PlanId]  INT         NOT NULL,
        [NodeKey] VARCHAR(80)  NOT NULL,
        CONSTRAINT [PK_PlanNodes] PRIMARY KEY CLUSTERED ([PlanId], [NodeKey]),
        CONSTRAINT [FK_PlanNodes_Plans] FOREIGN KEY ([PlanId]) REFERENCES [subscriptions].[Plans] ([Id]) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'subscriptions.Subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE [subscriptions].[Subscriptions]
    (
        [Id]          BIGINT       IDENTITY(1,1) NOT NULL,
        [OwnerType]   VARCHAR(20)  NOT NULL,
        [OwnerId]     BIGINT       NOT NULL,
        [PlanId]      INT          NOT NULL,
        [StartsAtUtc] DATETIME2(3) NOT NULL,
        [EndsAtUtc]   DATETIME2(3) NOT NULL,
        [Status]      VARCHAR(20)  NOT NULL CONSTRAINT [DF_Subscriptions_Status] DEFAULT ('Active'),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Subscriptions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Subscriptions_Plans] FOREIGN KEY ([PlanId]) REFERENCES [subscriptions].[Plans] ([Id]),
        CONSTRAINT [CK_Subscriptions_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );
END
GO

IF COL_LENGTH(N'subscriptions.Subscriptions', N'UserId') IS NOT NULL
   AND COL_LENGTH(N'subscriptions.Subscriptions', N'OwnerType') IS NULL
    EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] ADD [OwnerType] VARCHAR(20) NULL, [OwnerId] BIGINT NULL');
GO

IF COL_LENGTH(N'subscriptions.Subscriptions', N'UserId') IS NOT NULL
   AND COL_LENGTH(N'subscriptions.Subscriptions', N'OwnerType') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [subscriptions].[Subscriptions] SET [OwnerType] = ''User'', [OwnerId] = [UserId] WHERE [OwnerType] IS NULL');
    EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] ALTER COLUMN [OwnerType] VARCHAR(20) NOT NULL');
    EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] ALTER COLUMN [OwnerId] BIGINT NOT NULL');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Subscriptions_OwnerType')
        EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] ADD CONSTRAINT [CK_Subscriptions_OwnerType] CHECK ([OwnerType] IN (''User'', ''Company''))');
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Subscriptions_Users')
        EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] DROP CONSTRAINT [FK_Subscriptions_Users]');
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_UserId')
        EXEC(N'DROP INDEX [IX_Subscriptions_UserId] ON [subscriptions].[Subscriptions]');
    EXEC(N'ALTER TABLE [subscriptions].[Subscriptions] DROP COLUMN [UserId]');
END
GO
