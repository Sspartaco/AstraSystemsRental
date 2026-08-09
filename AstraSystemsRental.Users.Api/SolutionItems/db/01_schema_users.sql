USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'users.Persons', N'U') IS NULL
BEGIN
    CREATE TABLE [users].[Persons]
    (
        [Id]             BIGINT        IDENTITY(1,1) NOT NULL,
        [FirstNames]     NVARCHAR(120) NOT NULL,
        [LastNames]      NVARCHAR(120) NOT NULL,
        [Address]        NVARCHAR(250) NULL,
        [PersonType]     VARCHAR(20)   NOT NULL,
        [DocumentNumber] VARCHAR(40)   NOT NULL,
        [CompanySize]    VARCHAR(20)   NULL,
        [Email]          NVARCHAR(256) NOT NULL,
        [CreatedAtUtc]   DATETIME2(3)  NOT NULL CONSTRAINT [DF_Persons_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Persons] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_Persons_PersonType] CHECK ([PersonType] IN ('Natural', 'Juridico'))
    );
END
GO

IF OBJECT_ID(N'users.Users', N'U') IS NULL
BEGIN
    CREATE TABLE [users].[Users]
    (
        [Id]           BIGINT        IDENTITY(1,1) NOT NULL,
        [PersonId]     BIGINT        NOT NULL,
        [Email]        NVARCHAR(256) NOT NULL,
        [PasswordHash] VARBINARY(256) NULL,
        [PasswordSalt] VARBINARY(128) NULL,
        [RoleId]       INT           NOT NULL,
        [IsActive]     BIT           NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (0),
        [IsConfirmed]  BIT           NOT NULL CONSTRAINT [DF_Users_IsConfirmed] DEFAULT (0),
        [CreatedAtUtc] DATETIME2(3)  NOT NULL CONSTRAINT [DF_Users_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Users_Persons] FOREIGN KEY ([PersonId]) REFERENCES [users].[Persons] ([Id]),
        CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [access].[Roles] ([Id])
    );
END
GO

IF OBJECT_ID(N'users.EmailConfirmations', N'U') IS NULL
BEGIN
    CREATE TABLE [users].[EmailConfirmations]
    (
        [Id]           BIGINT       IDENTITY(1,1) NOT NULL,
        [UserId]       BIGINT       NOT NULL,
        [Token]        VARCHAR(128) NOT NULL,
        [ExpiresAtUtc] DATETIME2(3) NOT NULL,
        [ConsumedAtUtc] DATETIME2(3) NULL,
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_EmailConfirmations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailConfirmations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_EmailConfirmations_Users] FOREIGN KEY ([UserId]) REFERENCES [users].[Users] ([Id]) ON DELETE CASCADE
    );
END
GO
